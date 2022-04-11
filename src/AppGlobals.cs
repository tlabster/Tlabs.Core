﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
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
    public static readonly string FrameworkVersion;
    ///<summary>Default format provider.</summary>
    public static readonly System.Globalization.CultureInfo DfltFormat= System.Globalization.CultureInfo.InvariantCulture;

    static readonly Lazy<IConfigurationRoot> cfgSettings;
    // static IWebHost host;
    static ILoggerFactory logFactory;
    static ILoggerFactory tmpLogFactory;
    static readonly IHostApplicationLifetime notYetALife= new NotYetALifeApplication();
    static IHostApplicationLifetime appLifetime= notYetALife;
    static IServiceProvider appSvcProv;

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
      Console.WriteLine($"++ Using temporary default {nameof(LoggerFactory)}");
      return tmpLogFactory= LoggerFactory.Create(builder => {
        builder.AddConsole(opt => {
          // opt.TimestampFormat= "???";
        });
        builder.SetMinimumLevel(LogLevel.Trace);
      });
    }

    ///<summary>Returns a <see cref="ILogger{T}"/> for <typeparamref name="T"/></summary>
    ///<remarks>
    ///The logger returned is internally managed as singleton.
    ///</remarks>
    public static ILogger<T> Logger<T>() { return Singleton<AppLogger<T>>.Instance; }

    ///<summary>Application <see cref="IHostApplicationLifetime"/></summary>
    public static IHostApplicationLifetime AppLifetime {
      get { return appLifetime; }
    }

    ///<summary>Application wide <see cref="IServiceProvider"/></summary>
    ///<remarks>
    /// This should be set (once) during application startup.
    ///</remarks>
    public static IServiceProvider ServiceProv {
      get { return appSvcProv; }
      set {
        if (null != appSvcProv || null != Interlocked.CompareExchange<IServiceProvider>(ref appSvcProv, value, null)) throw new InvalidOperationException($"{nameof(ServiceProv)} is already set.");
        Interlocked.CompareExchange<IHostApplicationLifetime>(ref appLifetime, appSvcProv.GetService(typeof(IHostApplicationLifetime)) as IHostApplicationLifetime, notYetALife);
      }
    }

    ///<summary>Exceutes the <paramref name="scopedAction"/> with a (new) scoped <see cref="IServiceProvider"/>.</summary>
    public static void WithServiceScope(Action<IServiceProvider> scopedAction) {
      var scopeFac= ServiceProv.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
      using var svcScope= scopeFac?.CreateScope();
      scopedAction(svcScope.ServiceProvider);
    }

    ///<summary>Create a new instance of <paramref name="instanceType"/> with any service dependencies from a suitable ctor
    ///resolved from the optional <paramref name="svcProv"/> (defaults to <see cref="ServiceProv"/>).
    ///</summary>
    public static object CreateResolvedInstance(Type instanceType, IServiceProvider svcProv= null) => ActivatorUtilities.CreateInstance(svcProv ?? ServiceProv, instanceType);

    ///<summary>Runs an asynchronous background service by calling <paramref name="runSvc"/>.</summary>
    ///<typeparam name="TSvc">Type of the service being created.</typeparam>
    ///<typeparam name="TRes">Type of the service result (returned from <paramref name="runSvc"/>).</typeparam>
    ///<remarks>
    ///An instance of the service of type <typeparamref name="TSvc"/> is created using a ctor whose parameters are getting resolved via dependency injection from <see cref="App.ServiceProv"/>.
    ///(It is also okay if <typeparamref name="TSvc"/> only has a default ctor.)
    ///<para>NOTE: If <typeparamref name="TSvc"/> is <see cref="IDisposable"/> it gets disposed after <paramref name="runSvc"/> was invoked.</para>
    ///</remarks>
    public static Task<TRes> RunBackgroundService<TSvc, TRes>(Func<TSvc, TRes> runSvc) where TRes : class {
      return RunBackgroundService<TSvc, TRes>(typeof(TSvc), runSvc);
    }

    ///<summary>Runs an asynchronous background service by calling <paramref name="runSvc"/>.</summary>
    ///<typeparam name="TSvc">Type of the service being created.</typeparam>
    ///<typeparam name="TRes">Type of the service result (returned from <paramref name="runSvc"/>).</typeparam>
    ///<remarks>
    ///An instance of the service of type <paramref name="svcType"/> is created using a ctor whose parameters are getting resolved via dependency injection from <see cref="App.ServiceProv"/>.
    ///<para>This overload of the method is to be used in cases where <paramref name="svcType"/> is not known at compile time, but must be assignable to <typeparamref name="TSvc"/>.</para>
    ///(It is also okay if <paramref name="svcType"/> only has a default ctor.)
    ///<para>NOTE: If <paramref name="svcType"/> is <see cref="IDisposable"/> it gets disposed after <paramref name="runSvc"/> was invoked.</para>
    ///</remarks>
    public static Task<TRes> RunBackgroundService<TSvc, TRes>(Type svcType, Func<TSvc, TRes> runSvc) where TRes : class {
      // return Task<TRes>.Run(() => {
      TRes service() {
        TRes res= null;
        WithServiceScope(svcProv => {
          TSvc svc= default;
          try {
            svc= (TSvc)CreateResolvedInstance(svcType, svcProv);
            res= runSvc(svc);
          }
          finally { (svc as IDisposable)?.Dispose(); }   //try to dispose
        });
        return res;
      }
      // return Task.Run(service);
      return Task.Factory.StartNew(service,
                                     TaskCreationOptions.DenyChildAttach    //default from Task.Run(service)
                                   | TaskCreationOptions.PreferFairness     //prefer parallel exec. (by scheduling on the global queue insted of the thread local queue)
      //                           | TaskCreationOptions.LongRunning        //hint to create a new thread w/o consuming a thread-pool thread
      );
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
      readonly ILogger<T> log;
      public AppLogger() {
        this.log= App.LogFactory.CreateLogger<T>();
      }
      public IDisposable BeginScope<TState>(TState state) { return log.BeginScope(state); }
      public bool IsEnabled(LogLevel logLevel) { return log.IsEnabled(logLevel); }
      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
        log.Log<TState>(logLevel, eventId, state, exception, formatter);
      }
    }

    private class NotYetALifeApplication : IHostApplicationLifetime {
      public CancellationToken ApplicationStarted => throw new NotImplementedException();

      public CancellationToken ApplicationStopping => throw new NotImplementedException();

      public CancellationToken ApplicationStopped => throw new NotImplementedException();

      public void StopApplication() {
        throw new NotImplementedException();
      }
    }
  }
}
