using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tlabs.Config;

namespace Tlabs {
  using RTinfo = System.Runtime.InteropServices.RuntimeInformation;

  ///<summary>Class to facillitate the creation of the application's start-up configuration expressed as a pre-configured <see cref="IHostBuilder"/>.</summary>
  ///<remarks>The ambition of this <see cref="ApplicationStartup"/> class is to reducee hard-coded pre-configuration to an absolute minimum with favor of
  ///leveraging the <see cref="IConfigurator{T}"/> base <c>ApplyConfigurator(...)</c> pattern.
  ///</remarks>
  public sealed class ApplicationStartup {
    ///<summary>
    /// Environment variable to control loading of an additional settings file with the naming pattern:
    /// <code>appsettings.{%APP_ENV_VAR%}.json</code>.
    ///</summary>
    ///<example>
    ///   appsettings.Development.json
    ///   appsettings.Azure.json
    ///</example>
    public const string APP_ENV_VAR= "ASPNETCORE_ENVIRONMENT";
    ///<summary>Default section of the configuratiion to be used for the start-up hosting environment setup.</summary>
    public const string DFLT_HOST_SECTION= "appHosting";
    ///<summary>Application services config section.</summary>
    public const string APP_SVC_SECTION= "applicationServices";
    internal const string ENV_DOTNET_PFX= "DOTNET_";
    internal const string ENV_ASPNET_PFX= "ASPNET_";
    static readonly Assembly? entryAsm= Assembly.GetEntryAssembly();

    ///<summary>Create a <see cref="IHostBuilder"/> from optional command line <paramref name="args"/>.</summary>
    public static IHostBuilder CreateAppHostBuilder(string[]? args= null, Action<IHostBuilder, IConfiguration>? cfgBuilder= null) => CreateAppHostBuilder(DFLT_HOST_SECTION, args, cfgBuilder);
    ///<summary>Create a <see cref="IHostBuilder"/> from <paramref name="hostSection"/> and command line <paramref name="args"/>.</summary>
    public static IHostBuilder CreateAppHostBuilder(string hostSection, string[]? args= null, Action<IHostBuilder, IConfiguration>? cfgBuilder= null) {
      /* Setup the configuration settings only if current settings are empty:
       * (The actual setings are composed according to ConfigUtilsExtensions.AddApplicationConfig()...)
       */
      if (!App.Settings.GetChildren().Any()) {
        var envPfx= null != entryAsm ? $"{entryAsm.GetName().Name}_" : null;
        App.Setup.Configuration.AddApplicationConfig(null, APP_ENV_VAR, args, envPfx);
      }
      var hostConfig= App.Settings.GetSection(hostSection);

      /* Setup a LogFactory only for a non-empty 'logging' configuration in section:
       */
      var logSection= App.Settings.GetSection("logging");
      if (logSection.GetChildren().Any()) App.Setup= App.Setup with {
        LogFactory= CreateLogFactory(logSection)
      };

      var appLog= InitLog<ApplicationStartup>(entryAsm);

      var hostBuilder= new HostBuilder(); //Host.CreateDefaultBuilder(args) [https://github.com/dotnet/runtime/blob/79ae74f5ca5c8a6fe3a48935e85bd7374959c570/src/libraries/Microsoft.Extensions.Hosting/src/Host.cs]
      hostBuilder.UseContentRoot(App.ContentRoot);
      hostBuilder.ConfigureHostConfiguration(hostCfg => hostCfg.AddConfiguration(hostConfig)
                                                               .AddEnvironmentVariables(prefix: ENV_DOTNET_PFX)
                                                               .AddEnvironmentVariables(prefix: ENV_ASPNET_PFX));

      /* Configure additional host settings:
       */
      cfgBuilder?.Invoke(hostBuilder, App.Settings.GetSection(hostSection));

      /* Configure DI service provider (with validation in development environment).
       */
      hostBuilder.UseDefaultServiceProvider((hostingCtx, options) => {
        bool isDevelopment= hostingCtx.HostingEnvironment.IsDevelopment();
        options.ValidateScopes= isDevelopment;
        options.ValidateOnBuild= isDevelopment;
      });

      hostBuilder.ConfigureServices((hostingCtx, services) => {
        services.AddOptions();

        /* Add the logFactory as service:
         */
        services.AddSingleton<ILoggerFactory>(App.LogFactory);
        //services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(typeof(ILogger<>), typeof(Tlabs.App.SngLogger<>));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
          new DefaultLoggerLevelConfigureOptions(LogLevel.Information))
        );

        // services.AddMetrics(metrics => {
        //   metrics.AddConfiguration(hostingContext.Configuration.GetSection("Metrics"));
        // });

        /* Add application service(s) from configuration:
         */
        services.ApplyConfigurators(App.Settings, Tlabs.ApplicationStartup.APP_SVC_SECTION);
      });

      return new AppHostBuilder(hostBuilder);
    }

    /// <summary>Create a <see cref="ILoggerFactory"/> from config section <paramref name="logConfig"/></summary>
    public static ILoggerFactory CreateLogFactory(IConfigurationSection logConfig) => LoggerFactory.Create(builder => {
      builder.AddConfiguration(logConfig);
      builder.ApplyConfigurators(logConfig, "configurator");
    });

    ///<summary>Creates an application logger (and intital log output).</summary>
    ///<param name="entryAsm">Entry assembly</param>
    ///<typeparam name="T">Application type</typeparam>
    public static ILogger<T> InitLog<T>(Assembly? entryAsm = null) {
      entryAsm??= Assembly.GetEntryAssembly();
      var log= App.Logger<T>();
      log.LogCritical(        //this is the very first log entry
        "*** {appName}\n" +
        "\t({path})\n" +
        "\ton {netVers} ({arch})\n" +
        "\t - {os}",
        entryAsm?.FullName??"?APP",
        App.MainEntryPath,
        $"{RTinfo.FrameworkDescription} framwork", RTinfo.OSArchitecture,
        RTinfo.OSDescription);
      return log;
    }


    private class AppHostBuilder(IHostBuilder hostBuilder) : IHostBuilder {
      public IHost Build() {
        var host= hostBuilder.Build();
        App.Setup= App.Setup with { ServiceProv= host.Services };
        App.AppLifetime.ApplicationStopped.Register(onShutdown);

        return host;
      }
      public IDictionary<object, object> Properties => hostBuilder.Properties;
      public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate) => hostBuilder.ConfigureAppConfiguration(configureDelegate);
      public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) => hostBuilder.ConfigureContainer(configureDelegate);
      public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate) => hostBuilder.ConfigureHostConfiguration(configureDelegate);
      public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate) => hostBuilder.ConfigureServices(configureDelegate);
      public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull => hostBuilder.UseServiceProviderFactory(factory);
      public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull => hostBuilder.UseServiceProviderFactory(factory);

      private static void onShutdown() {
        App.Logger<ApplicationStartup>().LogCritical("Shutdown.\n\n");
        Serilog.Log.CloseAndFlush();
      }
    }

    sealed class DefaultLoggerLevelConfigureOptions : ConfigureOptions<LoggerFilterOptions> {
      public DefaultLoggerLevelConfigureOptions(LogLevel level) : base(options => options.MinLevel = level) { }
    }

#pragma warning disable IDE0051   //keep in case we need this one day
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
