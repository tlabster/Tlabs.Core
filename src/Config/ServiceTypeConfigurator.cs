using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tlabs.Config {
  ///<summary>Configures services specified by assembly qualified names.</summary>
  ///<remarks>
  ///The syntax for specifying the service type and its implementation type for registration as service is:
  ///<code>
  ///...
  ///"config": {
  ///  "serviceDesc": " {qualifiedTypeName-of-service-type} | {qualifiedTypeName-of-implementation-type} "
  ///}
  ///</code>
  ///NOTE: Service get registered by sort order of their "serviceDesc" key.
  ///</remarks>
  public class ServiceTypeConfigurator : IConfigurator<IServiceCollection> {
    IDictionary<string, string> config;

    ///<summary>Ctor from <paramref name="config"/>.</summary>
    public ServiceTypeConfigurator(IDictionary<string, string> config) {
      this.config= config ?? new Dictionary<string, string>(0);
    }

    ///<inherit/>
    public void AddTo(IServiceCollection services, IConfiguration cfg) {
      foreach (var pair in config.OrderBy(p => p.Key)) {
        var parts= pair.Value.Split('|');
        if (parts.Length > 2) throw new AppConfigException($"Invalid service type descriptor {pair.Key}: '{pair.Value}");
        if (1 == parts.Length) parts= new string[] {parts[0], parts[0]};
        var svcType= Misc.Safe.LoadType(parts[0].Trim(), pair.Key + "-" + "serviceType");
        var implType= Misc.Safe.LoadType(parts[1].Trim(), pair.Key + "-" + "implType");
        services.AddScoped(svcType, implType);
      }
    }
  }
}