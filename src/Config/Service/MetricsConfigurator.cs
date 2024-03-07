using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Tlabs.Config {
  ///<summary>Configures metrics.</summary>
  public class MetricsConfigurator : IConfigurator<IServiceCollection> {
    readonly IDictionary<string, string> config;

    ///<summary>Ctor from <paramref name="config"/>.</summary>
    public MetricsConfigurator(IDictionary<string, string> config) {
      this.config= config ?? new Dictionary<string, string>(0);
    }

    ///<inheritdoc/>
    public void AddTo(IServiceCollection services, IConfiguration cfg) {
      config.TryGetValue("section", out var section);
      services.AddMetrics(metrics => {
        if (!string.IsNullOrEmpty(section)) metrics.AddConfiguration(App.Settings.GetSection(section));
      });
    }
  }
}