using System;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Tlabs.Misc;
using Tlabs.Config;
using System.Reflection;


namespace Tlabs {
  using RTinfo = System.Runtime.InteropServices.RuntimeInformation;

  /// <summary>Comon application's setup</summary>
  /// <remarks>
  ///
  /// </remarks>
  /// <param name="ContentRoot">App. content root path</param>
  /// <param name="ConfigMngr">App. (root) configuration manager</param>
  /// <param name="LogFactory">App. <see cref="ILoggerFactory"/></param>
  /// <param name="ServiceProv">App. <see cref="IServiceProvider"/></param>
  /// <param name="TimeInfo"></param>
  public record class ApplicationSetup(
    string ContentRoot,
    ConfigurationManager ConfigMngr,
    ILoggerFactory LogFactory,
    IServiceProvider ServiceProv,
    IAppTime TimeInfo
  ) {
    internal class DEFAULT_ServiceProvider : IServiceProvider {
      public object? GetService(Type serviceType) {
        App.Logger<DEFAULT_ServiceProvider>().LogWarning(nameof(DEFAULT_ServiceProvider) + " DOES NOT PROVIDE ANY SERVICE: {type}", serviceType?.Name);
        return null;
      }
    }

    static ILoggerFactory DEFAULT_LoggerFactory= CreateConsoleLoggerFactory(new() {
      TimestampFormat= "HH:mm:ss.fff",
      DfltMinimumLevel= LogLevel.Trace
    });

    /// <summary>Default application setup</summary>
    public readonly static ApplicationSetup Default= Singleton<ApplicationSetup>.Instance;

    /// <summary>Default ctor</summary>
    public ApplicationSetup() : this(
      ContentRoot:  Directory.GetCurrentDirectory(),
      ConfigMngr:   new(),
      LogFactory:   DEFAULT_LoggerFactory,
      ServiceProv:  Singleton<DEFAULT_ServiceProvider>.Instance,
      TimeInfo:     Singleton<DateTimeHelper>.Instance
    ) { }


    /// <summary>Create a <see cref="ILoggerFactory"/> from config section <paramref name="logConfig"/></summary>
    public static ILoggerFactory CreateLogFactory(IConfigurationSection logConfig) => LoggerFactory.Create(builder => {
      builder.AddConfiguration(logConfig);
      builder.ApplyConfigurators(logConfig, "configurator");
    });

    /// <summary>Create a console <see cref="ILoggerFactory"/> from <paramref name="options"/></summary>
    public static ILoggerFactory CreateConsoleLoggerFactory(CustomStdoutFormatterOptions? options= null) => LoggerFactory.Create(builder => {
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

    ///<summary>Creates an application logger (and intital log output).</summary>
    ///<param name="entryAsm">Entry assembly</param>
    ///<typeparam name="T">Application type</typeparam>
    public static ILogger<T> InitLog<T>(Assembly? entryAsm= null) {
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

  }
}