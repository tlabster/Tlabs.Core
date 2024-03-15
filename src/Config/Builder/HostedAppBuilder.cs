using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tlabs.Config {

  /// <summary>Hosted <see cref="IHostApplicationBuilder"/> builder.</summary>
  public class HostedAppBuilder : BaseHostedAppBuilder {
    internal const string DFLT_HOST_SECTION= "appHosting";

    class AppBuilderFactory : IHostedBuilderFactory {
      public IHostApplicationBuilder Create(IConfigurationSection hostConfig, string[]? args) {
        var config= new ConfigurationManager();
        config.AddConfiguration(hostConfig);
        return new HostApplicationBuilder(new HostApplicationBuilderSettings {
          DisableDefaults= true,
          Configuration= config,
          EnvironmentName= App.Setup.EnvironmentName,
          ContentRootPath= App.ContentRoot,
          ApplicationName= App.Setup.Name
        });
      }
    }

    /// <summary>Ctor from optional <paramref name="args"/>.</summary>
    public HostedAppBuilder(string[]? args= null) : this(DFLT_HOST_SECTION, args) { }
    /// <summary>Ctor from optional <paramref name="hostSectionName"/> and <paramref name="args"/>.</summary>
    public HostedAppBuilder(string hostSectionName, string[]? args= null)
      : base(new AppBuilderFactory(), hostSectionName, args) { }

    /// <summary>Build <see cref="IHost"/>.</summary>
    public IHost Build() {
      var host= ((HostApplicationBuilder)hostedAppBuilder).Build();
      this.setupHost(host);

      return host;
    }
  }
}
