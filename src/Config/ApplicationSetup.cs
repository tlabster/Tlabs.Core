using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Tlabs.Misc;

namespace Tlabs.Config {
  using RTinfo= System.Runtime.InteropServices.RuntimeInformation;

  /// <summary>Comon application's setup</summary>
  /// <remarks>
  ///
  /// </remarks>
  /// <param name="Name">App. name</param>
  /// <param name="EnvironmentName">App. (hosting) environment name</param>
  /// <param name="ContentRoot">App. content root path</param>
  /// <param name="Configuration">App. (root) configuration manager</param>
  /// <param name="LogFactory">App. <see cref="ILoggerFactory"/></param>
  /// <param name="ServiceProv">App. <see cref="IServiceProvider"/></param>
  /// <param name="TimeInfo"></param>
  public record class ApplicationSetup(
    string Name,
    string EnvironmentName,
    string ContentRoot,
    ConfigurationManager Configuration,
    ILoggerFactory LogFactory,
    IServiceProvider ServiceProv,
    IAppTime TimeInfo
  ) {
    /// <summary>ASP.NET env. variable name to read the host environment</summary>
    public const string ASPNET_ENV_NAME= "ASPNETCORE_ENVIRONMENT";
    /// <summary>.NET env. variable name to read the host environment</summary>
    public const string DOTNET_ENV_NAME= "DOTNET_ENVIRONMENT";

    internal class EmptyServiceProvider : IServiceProvider {
      public object? GetService(Type serviceType) {
        App.Logger<EmptyServiceProvider>().LogWarning(nameof(EmptyServiceProvider) + " DOES NOT PROVIDE ANY SERVICE: {type}", serviceType?.Name);
        return null;
      }
    }

    internal static EmptyServiceProvider DEFAULT_ServiceProvider= Singleton<EmptyServiceProvider>.Instance;
    internal static ILoggerFactory DEFAULT_LoggerFactory= CreateBasicConsoleLoggerFactory(new() {
      TimestampFormat= "HH:mm:ss.fff",
      DfltMinimumLevel= LogLevel.Trace
    });

    /// <summary>Default application setup</summary>
    public readonly static ApplicationSetup Default= Singleton<ApplicationSetup>.Instance;

    /// <summary>Default ctor</summary>
    public ApplicationSetup() : this(
      Name: (Assembly.GetEntryAssembly() ?? Assembly.GetAssembly(typeof(ApplicationStartup)))?.GetName().Name ?? "?APP",
      EnvironmentName:    Environment.GetEnvironmentVariable(ASPNET_ENV_NAME)
                       ?? Environment.GetEnvironmentVariable(DOTNET_ENV_NAME)
                       ?? Environments.Production,
      ContentRoot:  AppContext.BaseDirectory, //Directory.GetCurrentDirectory(),
      Configuration:   new(),
      LogFactory:   DEFAULT_LoggerFactory,
      ServiceProv:  DEFAULT_ServiceProvider,
      TimeInfo:     Singleton<DateTimeHelper>.Instance
    ) {
      if (IsDevelopmentEnv) ContentRoot= Environment.CurrentDirectory;
    }

    /// <summary>True if development environemnt</summary>
    public bool IsDevelopmentEnv => EnvironmentName == Environments.Development;

    /// <summary>Apply application config settings only if current settings are empty</summary>
    /// <remarks>The actual setings are composed according to <see cref="ConfigUtilsExtensions.AddApplicationConfig(IConfigurationBuilder, string, string, string[], string)"/>...</remarks>
    /// <param name="args">optional cmd line args</param>
    public static void ApplyMissingApplicationConfig(string[]? args= null) {
      var setup= App.Setup;
      if (!setup.Configuration.GetChildren().Any()) {
        var envPfx= $"{setup.Name}_";
        App.Setup.Configuration.AddApplicationConfig(null, App.Setup.EnvironmentName, args, envPfx);
      }
    }

    /// <summary>Setup a LogFactory for a non-empty section with <paramref name="sectionName"/></summary>
    public static void SetupLogFactory(string sectionName) {
      var logSection= App.Settings.GetSection("logging");
      if (logSection.GetChildren().Any()) App.Setup= App.Setup with {
        LogFactory= CreateLogFactory(logSection)
      };
    }

    /// <summary>Create a <see cref="ILoggerFactory"/> from config section <paramref name="logConfig"/></summary>
    public static ILoggerFactory CreateLogFactory(IConfigurationSection logConfig) => LoggerFactory.Create(builder => {
      builder.AddConfiguration(logConfig);
      builder.ApplyConfigurators(logConfig, "configurator");
    });

    ///<summary>Creates an application logger (and intital log output) for <paramref name="tp"/>.</summary>
    public static ILogger InitLog(Type tp) {
      var log= App.LogFactory.CreateLogger(tp);
      log.LogCritical(        //this is the very first log entry
        "*** {appName}\n" +
        "\t({path})\n" +
        "\ton {netVers} ({arch})\n" +
        "\t - {os}",
        $"{App.Setup.Name} {Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0"}",
        App.MainEntryPath,
        $"{RTinfo.FrameworkDescription} framwork", RTinfo.OSArchitecture,
        RTinfo.OSDescription);
      log.LogDebug("Runtime environment: {env}", App.Setup.EnvironmentName);
      log.LogDebug("ContentRoot: {croot}", App.Setup.ContentRoot);
      return log;
    }

    ///<summary>Creates an application logger (and intital log output).</summary>
    ///<typeparam name="T">Application type</typeparam>
    public static ILogger<T> InitLog<T>() {
      return (ILogger<T>)InitLog(typeof(T));
    }

    /// <summary>Create a console <see cref="ILoggerFactory"/> from <paramref name="options"/></summary>
    public static ILoggerFactory CreateBasicConsoleLoggerFactory(CustomStdoutFormatterOptions? options= null) => LoggerFactory.Create(builder => {
      options??= new();
      builder.AddConsole(opt => opt.FormatterName= CustomStdoutFormatter.NAME)
             .AddConsoleFormatter<CustomStdoutFormatter, CustomStdoutFormatterOptions>(o => {
               o.IncludeCategory= options.IncludeCategory;
               o.IncludeScopes= options.IncludeScopes;
               o.TimestampFormat= options.TimestampFormat;//"HH:mm:ss.fff";
               o.UseUtcTimestamp= options.UseUtcTimestamp;
             });
      builder.SetMinimumLevel(options.DfltMinimumLevel);
    });

  }
}