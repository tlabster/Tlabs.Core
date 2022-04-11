using System;
using Microsoft.Extensions.DependencyInjection;

namespace Tlabs.Misc {

  ///<summary>Abstract <see cref="IServiceProvider"/> factory helper.</summary>
  ///<remarks>
  ///Creates a validating DI based <see cref="IServiceProvider"/>. (Most usefull for unit tests.)
  ///<para>
  ///Derived classes must configure <see cref="svcColl"/>!
  ///</para>
  ///</remarks>
  public abstract class AbstractServiceProviderFactory : IDisposable {
    ServiceProvider svcProv;
    ///<summary>Service collection to be configured from derived class.</summary>
    protected readonly IServiceCollection svcColl= new ServiceCollection();

    ///<summary>Service provider.</summary>
    public ServiceProvider SvcProv {
      get {
        lock (svcColl) {
          return this.svcProv??= CreateServiceProvider();
        }
      }
    }

    ///<summary>Create a scoped service provider.</summary>
    public IServiceScope CreateScope() => SvcProv?.GetRequiredService<IServiceScopeFactory>().CreateScope();

    ///<summary>Perform <paramref name="action"/> with new service provider scope.</summary>
    public void WithScope(Action<IServiceProvider> action) {
      using var scope = CreateScope();
      action(scope.ServiceProvider);
    }

    ///<inheritdoc/>
    public void Dispose() {
      DoDispose(true);
      GC.SuppressFinalize(this);
    }

    ///<summary>Dispose</summary>
    protected virtual void DoDispose(bool disposing) {
      if (!disposing) return;
      svcProv?.Dispose();
    }

    ///<summary>Create new service provider.</summary>
    ///<remarks>This gets invoked once before property <see cref="SvcProv"/> returns the first time.
    ///<para>To be used to add any initialisation once the ServiceProvider has been setup...</para></remarks>
    protected virtual ServiceProvider CreateServiceProvider() {
      var svcProv= svcColl.BuildServiceProvider(new ServiceProviderOptions {
        ValidateOnBuild= true,
        ValidateScopes= true
      });
      App.ServiceProv??= svcProv;
      return svcProv;
    }

  }
}