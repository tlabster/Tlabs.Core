using System;
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
    public const string APP_ENV_VAR= ApplicationStartup.APP_ENV_VAR;
    ///<summary>Config extension section.</summary>
    public const string XCFG_SECTION= ConfigUtilsExtensions.XCFG_SECTION;

    ///<summary>Main entry exe path.</summary>
    public static readonly string MainEntryPath;
    ///<summary>Current framework version.</summary>
    public static readonly string FrameworkVersion;
    ///<summary>Default format provider.</summary>
    public static readonly System.Globalization.CultureInfo DfltFormat= System.Globalization.CultureInfo.InvariantCulture;

    static ApplicationSetup appSetup= ApplicationSetup.Default;

    static App() {
      MainEntryPath= Assembly.GetEntryAssembly()?.Location ?? "";
      if ("" == MainEntryPath) MainEntryPath= System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
      FrameworkVersion= Environment.Version.ToString();  //Path.GetFileName(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location));
    }

    ///<summary>Content root path.</summary>
    public static string ContentRoot => Setup.ContentRoot;

    ///<summary>General application setup.</summary>
    public static ApplicationSetup Setup {
      get => App.appSetup;
      set {
        var sup= App.appSetup= value;
        if (ApplicationSetup.DEFAULT_ServiceProvider != sup.ServiceProv)  //avoid log warning from DEFAULT_ServiceProvider
          AppLifetime= sup.ServiceProv.GetService<IHostApplicationLifetime>() ?? AppLifetime;
      }
    }

    ///<summary>Configuration settings.</summary>
    public static IConfigurationRoot Settings => Setup.Configuration;

    ///<summary>Configuration entry.</summary>
    [Obsolete("Use configuration extension method", error: false)]
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
    public static ILoggerFactory LogFactory => Setup.LogFactory;

    ///<summary>Returns a <see cref="ILogger{T}"/> for <typeparamref name="T"/></summary>
    ///<remarks>
    ///The logger returned is internally managed as singleton.
    ///</remarks>
    public static ILogger<T> Logger<T>() { return Singleton<WrappedLogger<T>>.Instance; }

    ///<summary>Application <see cref="IHostApplicationLifetime"/></summary>
    public static IHostApplicationLifetime AppLifetime { get; private set; }= Singleton<NotYetALifeApplication>.Instance;

    ///<summary>Application wide <see cref="IServiceProvider"/></summary>
    ///<remarks>
    /// This should be set (once) during application startup.
    ///</remarks>
    public static IServiceProvider ServiceProv => Setup.ServiceProv;

    ///<summary>Exceutes the <paramref name="scopedAction"/> with a (new) scoped <see cref="IServiceProvider"/>.</summary>
    public static void WithServiceScope(Action<IServiceProvider> scopedAction) {
      var scopeFac= ServiceProv.GetRequiredService<IServiceScopeFactory>();
      using var svcScope= scopeFac.CreateScope();
      scopedAction(svcScope.ServiceProvider);
    }

    ///<summary>Exceutes the <paramref name="scopedFunc"/> with a service instance of type <typeparamref name="T"/> from a (new) service scope.</summary>
    ///<returns>The model of type <typeparamref name="M"/> returned from <paramref name="scopedFunc"/></returns>
    public static M FromScopedServiceInstance<T, M>(Func<IServiceProvider, T, M> scopedFunc, params object[] extraParams) {
      using var svcScope= ServiceProv.GetRequiredService<IServiceScopeFactory>().CreateScope();
      var svcInstance= ActivatorUtilities.CreateInstance<T>(svcScope.ServiceProvider, extraParams);
      return scopedFunc(svcScope.ServiceProvider, svcInstance);
    }

    ///<summary>Create a new instance of <paramref name="instanceType"/> with any service dependencies from a suitable ctor
    ///resolved from the optional <paramref name="svcProv"/> (defaults to <see cref="ServiceProv"/>).
    ///</summary>
    public static object CreateResolvedInstance(Type instanceType, IServiceProvider? svcProv= null) => ActivatorUtilities.CreateInstance(svcProv ?? ServiceProv, instanceType);

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
        TRes? res= null;
        WithServiceScope(svcProv => {
          TSvc? svc= default;
          try {
            svc= (TSvc)CreateResolvedInstance(svcType, svcProv);
            res= runSvc(svc);
          }
          finally { (svc as IDisposable)?.Dispose(); }   //try to dispose
        });
        return res ?? throw new InvalidOperationException($"Failed to invoke {runSvc.GetType().Name}");
      }
      // return Task.Run(service);
      return Task.Factory.StartNew(service,
                                   CancellationToken.None,
                                     TaskCreationOptions.DenyChildAttach    //default from Task.Run(service)
                                   | TaskCreationOptions.PreferFairness     //prefer parallel exec. (by scheduling on the global queue insted of the thread local queue)
      //                           | TaskCreationOptions.LongRunning        //hint to create a new thread w/o consuming a thread-pool thread
      , TaskScheduler.Default                                               //use thread pool
      );
    }

    ///<summary>Application <see cref="TimeZoneInfo"/></summary>
    public static IAppTime TimeInfo => Setup.TimeInfo;

    internal class WrappedLogger<T> : ILogger<T> {
      readonly ILogger<T> log= App.LogFactory.CreateLogger<T>();
      public IDisposable? BeginScope<TState>(TState state) where TState : notnull { return log.BeginScope(state); }
      public bool IsEnabled(LogLevel logLevel) { return log.IsEnabled(logLevel); }
      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        log.Log<TState>(logLevel, eventId, state, exception, formatter);
      }
    }

    internal class SngLogger<T> : ILogger<T> {
      readonly ILogger<T> log= App.Logger<T>();
      public IDisposable? BeginScope<TState>(TState state) where TState : notnull { return log.BeginScope(state); }
      public bool IsEnabled(LogLevel logLevel) { return log.IsEnabled(logLevel); }
      public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        log.Log<TState>(logLevel, eventId, state, exception, formatter);
      }
    }

    internal class DEFAULT_ServiceProvider : IServiceProvider {
      public object? GetService(Type serviceType) {
        App.Logger<DEFAULT_ServiceProvider>().LogWarning(nameof(DEFAULT_ServiceProvider) + " DOES NOT PROVIDE ANY SERVICE: {type}", serviceType?.Name);
        return null;
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
