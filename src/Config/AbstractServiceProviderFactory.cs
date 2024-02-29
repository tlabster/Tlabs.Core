using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading;
using System.Diagnostics;

namespace Tlabs.Config {

  ///<summary>Abstract <see cref="IServiceProvider"/> factory helper.</summary>
  ///<remarks>
  ///Creates a validating DI based <see cref="IServiceProvider"/>. (Most usefull for unit tests.)
  ///<para>
  ///Derived classes must configure <see cref="svcColl"/>!
  ///</para>
  ///NOTE: The <see cref="IServiceProvider"/> returned from property <see cref="SvcProv"/> is from then on also available via <see cref="App.ServiceProv"/>.
  ///</remarks>
  public abstract class AbstractServiceProviderFactory : IDisposable {
    int disposedCnt= 0;
    IServiceProvider? svcProv;
    ///<summary>Service collection to be configured from derived class.</summary>
    protected readonly IServiceCollection svcColl= new ServiceCollection();

    ///<summary>Service provider.</summary>
    public IServiceProvider SvcProv {
      get {
        lock (svcColl) {
          return this.svcProv??= CreateServiceProvider();
        }
      }
    }

    ///<summary>Create a scoped service provider.</summary>
    public IServiceScope CreateScope() => SvcProv.GetRequiredService<IServiceScopeFactory>().CreateScope();

    ///<summary>Perform <paramref name="action"/> with new service provider scope.</summary>
    public void WithScope(Action<IServiceProvider> action) {
      using var scope= CreateScope();
      action(scope.ServiceProvider);
    }

    ///<inheritdoc/>
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ///<summary>Dispose</summary>
    protected virtual void Dispose(bool disposing) {
      if (   !disposing
          || Interlocked.Increment(ref disposedCnt) > 1) return;
      (svcProv as IDisposable)?.Dispose();
      App.Setup= App.Setup with { ServiceProv= ApplicationSetup.Default.ServiceProv };
    }

    ///<summary>Create new service provider.</summary>
    ///<remarks>This gets invoked once before property <see cref="SvcProv"/> returns the first time.
    ///<para>To be used to add any initialisation once the ServiceProvider has been setup...</para></remarks>
    protected virtual IServiceProvider CreateServiceProvider() {
      svcColl.TryAddSingleton<ILoggerFactory>(App.LogFactory);
      svcColl.TryAddSingleton(typeof(ILogger<>), typeof(Tlabs.App.SngLogger<>));
      App.Setup= App.Setup with { ServiceProv= svcColl.BuildServiceProvider(new ServiceProviderOptions {
        ValidateOnBuild= true,
        ValidateScopes= true
      })};
      return App.ServiceProv;
    }

  }

  ///<summary>Debug Helper.</summary>
  public class DbgHelper {
    ///<summary>Current process info.</summary>
    public static string ProcInfo() {
      var proc= System.Diagnostics.Process.GetCurrentProcess();
      return $"process ID: {proc.Id} ({proc.ProcessName})";
    }
    ///<summary>Hard break in debugger.</summary>
    [Conditional("DEBUG")]
    public static void HardBreak() {
      App.Logger<DbgHelper>().LogCritical("Waiting for a debugger attaching to {pinfo}).", ProcInfo());
      while (!System.Diagnostics.Debugger.IsAttached) System.Threading.Thread.Sleep(700);
      System.Diagnostics.Debugger.Break();
    }

  }
}