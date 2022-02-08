using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tlabs.Config;

namespace Tlabs {
  using RTinfo= System.Runtime.InteropServices.RuntimeInformation;

  ///<summary>Class to facillitate the creation of the application's start-up configuration expressed as a pre-configured <see cref="IHostBuilder"/>.</summary>
  ///<remarks>The ambition of this <see cref="ApplicationStartup"/> class is to reducee hard-coded pre-configuration to an absolute minimum with favor of
  ///leveraging the <see cref="IConfigurator{T}"/> base <c>ApplyConfigurator(...)</c> pattern.
  ///</remarks>
  public sealed class ApplicationStartup {
    ///<summary>Default section of the configuratiion to be used for the start-up hosting environment setup.</summary>
    public const string DFLT_HOST_SECTION= "appHosting";
    ///<summary>Application services config section.</summary>
    public const string APP_SVC_SECTION= "applicationServices";
    const string ENV_DOTNET_PFX= "DOTNET_";
    const string ENV_ASPNET_PFX= "ASPNET_";
    static Assembly entryAsm= Assembly.GetEntryAssembly();
    static ILogger log;

    static ILoggerFactory logFactory;

    ///<summary>Create a <see cref="IHostBuilder"/> from optional command line <paramref name="args"/>.</summary>
    public static IHostBuilder CreateAppHostBuilder(string[] args= null, Action<IHostBuilder, IConfiguration> cfgBuilder= null) => CreateAppHostBuilder(DFLT_HOST_SECTION, args, cfgBuilder);
    ///<summary>Create a <see cref="IHostBuilder"/> from <paramref name="hostSection"/> and command line <paramref name="args"/>.</summary>
    public static IHostBuilder CreateAppHostBuilder(string hostSection, string[] args= null, Action<IHostBuilder, IConfiguration> cfgBuilder= null) {

      var hostSettings= App.Settings.GetSection(hostSection)
                                    .ToConfigurationBuilder()
                                    .AddCommandLine(args)
                                    .Build();
      /* Create the logging facillity (ILogFactory based)
       * for being availble immediately (even before the DI service provider has been setup...)
       * from App.Logger<T>
       */
      logFactory= createLogFactory(hostSettings);
      ApplicationStartup.log= App.Logger<ApplicationStartup>();             //startup logger


      var hostBuilder= new HostBuilder(); //Host.CreateDefaultBuilder(args) [https://github.com/dotnet/runtime/blob/79ae74f5ca5c8a6fe3a48935e85bd7374959c570/src/libraries/Microsoft.Extensions.Hosting/src/Host.cs]
      hostBuilder.UseContentRoot(App.ContentRoot);
      hostBuilder.ConfigureHostConfiguration(host => host.AddEnvironmentVariables(prefix: ENV_DOTNET_PFX));
      hostBuilder.ConfigureAppConfiguration((hostingCtx, config) => {
        config.AddEnvironmentVariables(prefix: ENV_ASPNET_PFX);
        // configureUserSecret(hostingCtx.HostingEnvironment, config);    //This was a default of CreateDefaultBuilder() - consider to invoke...
      });

      /* Configure additional host settings:
       */
      cfgBuilder?.Invoke(hostBuilder, hostSettings);
     
      /* Configure DI service provider (with validation in development environment).
       */
      hostBuilder.UseDefaultServiceProvider((hostingCtx, options) => {
          bool isDevelopment= hostingCtx.HostingEnvironment.IsDevelopment();
          options.ValidateScopes= isDevelopment;
          options.ValidateOnBuild= isDevelopment;
      });

      /* Add the logFactory as service:
       */
      hostBuilder.ConfigureServices((hostingCtx, services) => {
        services.AddOptions();

        services.AddSingleton<ILoggerFactory>(logFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
              new DefaultLoggerLevelConfigureOptions(LogLevel.Information)));         
      });
        
      return hostBuilder;
    }

    static ILoggerFactory createLogFactory(IConfigurationRoot config) {
      var logConfig= App.Settings.GetSection("logging");
      var logFac= LoggerFactory.Create(log => {
        log.AddConfiguration(logConfig);
        log.AddEventSourceLogger();
        log.ApplyConfigurators(logConfig, "configurator");
        log.Services.Configure<LoggerFactoryOptions>(opt => opt.ActivityTrackingOptions=   ActivityTrackingOptions.SpanId
                                                                                         | ActivityTrackingOptions.TraceId
                                                                                         | ActivityTrackingOptions.ParentId
        );
      });
      App.LogFactory= logFac;
      App.Logger<ApplicationStartup>().LogCritical(        //this is the very first log entry
        "*** {appName}\n" +
        "\t({path})\n" +
        "\ton {netVers} ({arch})\n" +
        "\t - {os}",
        entryAsm.FullName,
        entryAsm.Location,
        $"{RTinfo.FrameworkDescription} framwork", RTinfo.OSArchitecture,
        RTinfo.OSDescription);
      return logFac;
    }

    sealed class DefaultLoggerLevelConfigureOptions : ConfigureOptions<LoggerFilterOptions> {
      public DefaultLoggerLevelConfigureOptions(LogLevel level) : base(options => options.MinLevel = level) { }
    }

    static void configureUserSecret(IHostEnvironment env, IConfigurationBuilder config) {
      if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName)) {
        var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
        if (appAssembly != null) {
          config.AddUserSecrets(appAssembly, optional: true);
        }
      }
    }
  }
}
