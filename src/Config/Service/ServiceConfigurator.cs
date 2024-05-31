using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
    readonly IDictionary<string, string> config;

    ///<summary>Ctor from <paramref name="config"/>.</summary>
    public ServiceTypeConfigurator(IDictionary<string, string> config) {
      this.config= config ?? new Dictionary<string, string>(0);
    }

    ///<inheritdoc/>
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

  ///<summary>Extensions to configure services.</summary>
  public static class ServicesConfigExtension {
    ///<summary>Configure <see cref="ILoggerFactory"/> and <see cref="ILogger{T}"/> as services.</summary>
    public static void ConfigureLoggingServices(this IServiceCollection svcColl) {
      svcColl.RemoveAll<ILoggerFactory>();
      svcColl.AddSingleton<ILoggerFactory>(App.LogFactory);

      svcColl.RemoveAll(typeof(ILogger<>));
      //services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
      svcColl.AddSingleton(typeof(ILogger<>), typeof(Tlabs.App.SngLogger<>));
      svcColl.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
        new DefaultLoggerLevelConfigureOptions(LogLevel.Information))
      );
    }
    sealed class DefaultLoggerLevelConfigureOptions : ConfigureOptions<LoggerFilterOptions> {
      public DefaultLoggerLevelConfigureOptions(LogLevel level) : base(options => options.MinLevel = level) { }
    }

  }


}