using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Tlabs.Config;
using Tlabs.Misc;

namespace Tlabs {

  ///<summary>Application global services.</summary>
  public static class App {

    ///<summary>Base settings file name.</summary>
    public const string SETTING_BASE_FILENAME= "appsettings";
    ///<summary>
    /// Environment variable to control loading of an additional settings file with the naming pattern:
    /// <code>appsettings.{%APP_ENV_VAR%}.json</code>.
    ///</summary>
    ///<example>
    ///   appsettings.Development.json
    ///   appsettings.Azure.json
    ///</example>
    public const string APP_ENV_VAR= "ASPNETCORE_ENVIRONMENT";
    ///<summary>Config extension section.</summary>
    public const string XCFG_SECTION= "configExtensions";

    ///<summary>Content root path.</summary>
    public static readonly string ContentRoot;
    ///<summary>Main entry exe path.</summary>
    public static readonly string MainEntryPath;
    ///<summary>Current framework version.</summary>
    public static string FrameworkVersion;
    static readonly Lazy<IConfigurationRoot> cfgSettings;
    static IWebHost host;
    static ILoggerFactory logFactory;
    static ILoggerFactory tmpLogFactory;
    static readonly IApplicationLifetime notYetALife= new NotYetALifeApplication();
    static IApplicationLifetime appLifetime= notYetALife;
    static IServiceProvider svcProv;

    static IAppTime appTime;

    static App() {
      ContentRoot= Directory.GetCurrentDirectory();
      MainEntryPath= Assembly.GetEntryAssembly().Location;
      FrameworkVersion= Path.GetFileName(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location));

      //Lazy configuration loading
      cfgSettings= new Lazy<IConfigurationRoot>(() => {
        var config= ConfigUtilsExtensions.LoadJson(
                      SETTING_BASE_FILENAME,
                      ContentRoot,
                      Environment.GetEnvironmentVariable(APP_ENV_VAR)
                    );
       var baseSettings= config.Build();
       config.ApplyConfigurators(baseSettings, XCFG_SECTION);
       return config.Build();
      });
    }

    ///<summary>Configuration settings.</summary>
    public static IConfigurationRoot Settings {
      get => cfgSettings.Value;
    }

    ///<summary>Configuration entry.</summary>
    public static string SettingsEntry(params string[] key) {
      var path= ConfigurationPath.Combine(key);
      return    App.Settings[path]
             ?? $"--[ {path} ]--";
    }

    ///<summary>The application executing host.</summary>
    public static IWebHost Host {
      get => host;
      set {
        if (null != host || null != Interlocked.CompareExchange<IWebHost>(ref host, value, null)) throw new InvalidOperationException($"{nameof(Host)} was already set.");
      }
    }

    ///<summary>A LogFactory</summary>
    ///<remarks>
    ///<para>This is the central <see cref="ILoggerFactory"/> that has been setup for the application.</para>
    ///NOTE: To obtain a singleton instance of <see cref="ILogger{T}"/> for type T it is best to call <see cref="App.Logger{T}()"/>
    ///</remarks>
    public static ILoggerFactory LogFactory {
      get => getOrInitLogFact(CreateDefaultLogFactory);
      set => getOrInitLogFact(() => value, true);
    }

    private static ILoggerFactory getOrInitLogFact(Func<ILoggerFactory> loggerFact, bool setOnce= false) {
      if (setOnce || null == logFactory) {
        var old= Interlocked.CompareExchange<ILoggerFactory>(ref logFactory, loggerFact(), null);
        if (   setOnce
            && null != old
            && tmpLogFactory != Interlocked.CompareExchange<ILoggerFactory>(ref logFactory, loggerFact(), tmpLogFactory)
        ) throw new InvalidOperationException($"{nameof(LogFactory)} is already set.");
      }
      return logFactory;
    }

    static ILoggerFactory CreateDefaultLogFactory() {
      // Console.WriteLine($"Using temporary default {nameof(LoggerFactory)}");
      return tmpLogFactory= new LoggerFactory()
        .AddConsole(Settings.GetSection("logging"));
        // .AddDebug(LogLevel.Trace);
    }

    ///<summary>Returns a <see cref="ILogger{T}"/> for <typeparamref name="T"/></summary>
    ///<remarks>
    ///The logger returned is internally managed as singleton.
    ///</remarks>
    public static ILogger<T> Logger<T>() { return Singleton<AppLogger<T>>.Instance; }

    ///<summary>Application <see cref="IApplicationLifetime"/></summary>
    public static IApplicationLifetime AppLifetime {
      get { return appLifetime; }
    }

    ///<summary>Application wide <see cref="IServiceProvider"/></summary>
    ///<remarks>
    /// This should be set (once) during application startup.
    ///</remarks>
    public static IServiceProvider ServiceProv {
      get { return svcProv; }
      set {
        if (null != svcProv || null != Interlocked.CompareExchange<IServiceProvider>(ref svcProv, value, null)) throw new InvalidOperationException($"{nameof(ServiceProv)} is already set.");
        Interlocked.CompareExchange<IApplicationLifetime>(ref appLifetime, svcProv.GetService(typeof(IApplicationLifetime)) as IApplicationLifetime, notYetALife);
      }
    }

    ///<summary>Exceutes the <paramref name="scopedAction"/> with a (new) scoped <see cref="IServiceProvider"/>.</summary>
    public static void WithServiceScope(Action<IServiceProvider> scopedAction) {
      var scopeFac= svcProv.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
      using(var svcScope= scopeFac?.CreateScope()) {
        scopedAction(svcScope.ServiceProvider);
      }
    }

    ///<summary>Exceutes the <paramref name="scopedAction"/> with a service instance of <typeparamref name="T"/> from a new scope.</summary>
    public static void WithScopedObject<T>(Action<T> scopedAction) {
      WithServiceScope(svcProv => scopedAction(ActivatorUtilities.CreateInstance<T>(svcProv)));
    }

    ///<summary>Application <see cref="TimeZoneInfo"/></summary>
    public static IAppTime TimeInfo {
      get {
        //if (null == appTime) throw new InvalidOperationException("Application time-zone info not set.");
        return appTime ?? new DateTimeHelper();
      }
      set {
        if (   null != appTime 
            || null != Interlocked.CompareExchange<IAppTime>(ref appTime, value, null)) throw new InvalidOperationException($"{nameof(appTime)} already determined.");
      }
    }

    internal class AppLogger<T> : ILogger<T> {
      private ILogger<T> log;
      public AppLogger() {
        this.log= App.LogFactory.CreateLogger<T>();
      }
      public IDisposable BeginScope<TState>(TState state) { return log.BeginScope(state); }
      public bool IsEnabled(LogLevel logLevel) { return log.IsEnabled(logLevel); }
      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
        log.Log<TState>(logLevel, eventId, state, exception, formatter);
      }
    }

    private class NotYetALifeApplication : IApplicationLifetime {
      public CancellationToken ApplicationStarted => throw new NotImplementedException();

      public CancellationToken ApplicationStopping => throw new NotImplementedException();

      public CancellationToken ApplicationStopped => throw new NotImplementedException();

      public void StopApplication() {
        throw new NotImplementedException();
      }
    }
  }
}
