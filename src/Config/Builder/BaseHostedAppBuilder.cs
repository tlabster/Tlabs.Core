using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Tlabs.Config {

  /// <summary>Factory of a <see cref="IHostApplicationBuilder"/>.</summary>
  public interface IHostedBuilderFactory {
    /// <summary>Create <see cref="IHostApplicationBuilder"/>.</summary>
    public IHostApplicationBuilder Create(IConfigurationSection hostConfig, string[]? args);
  }

  /// <summary>Base implementation of an (hosted) <see cref="IHostApplicationBuilder"/> builder.</summary>
  /// <remarks>This basically coordinates the adjustment of the <see cref="ApplicationSetup"/> according to the configuration defined with an <c>appsettings.json</c>.
  /// <para>The actual creation of an <see cref="IHostApplicationBuilder"/> is deferred to the <see cref="IHostedBuilderFactory"/>.
  /// </para>
  /// </remarks>
  public abstract class BaseHostedAppBuilder : IHostApplicationBuilder {
    internal const string ENV_ASPNET_PFX= "ASPNET_";
    internal const string ENV_DOTNET_PFX= "DOTNET_";

    /// <summary><see cref="IConfigurationSection"/></summary>
    protected IConfigurationSection hostConfig;
    /// <summary>IHostApplicationBuilder</summary>
    protected IHostApplicationBuilder hostedAppBuilder;
    /// <summary><see cref="ILogger"/></summary>
    protected ILogger log;

    /// <summary>Ctor from optional <paramref name="builderFactory"/>, <paramref name="hostSectionName"/> and optional <paramref name="args"/>.</summary>
    public BaseHostedAppBuilder(IHostedBuilderFactory builderFactory, string hostSectionName, string[]? args= null) {

      ApplicationSetup.ApplyMissingApplicationConfig(args);

      ApplicationSetup.SetupLogFactory("logging");

      log= ApplicationSetup.InitLog<BaseHostedAppBuilder>();

      this.hostConfig= App.Settings.GetSection(hostSectionName);

      this.hostedAppBuilder= builderFactory.Create(hostConfig, args);

      this.hostedAppBuilder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions {
        ValidateOnBuild= HostEnv.IsDevelopment(),
        ValidateScopes= HostEnv.IsDevelopment()
      }));

      this.hostedAppBuilder.Configuration.AddEnvironmentVariables(prefix: ENV_ASPNET_PFX)
                                         .AddEnvironmentVariables(prefix: ENV_DOTNET_PFX)
                                         .AddCommandLine(args ?? Array.Empty<string>());

      /* Add the logFactory as service:
       */
      this.hostedAppBuilder.Services.ConfigureLoggingServices();

      /* Add application service(s) from configuration:
       */
      this.hostedAppBuilder.Services.ApplyConfigurators(App.Settings, Tlabs.ApplicationStartup.APP_SVC_SECTION);

    }


    ///<inheritdoc/>
    public IDictionary<object, object> Properties => ((IHostApplicationBuilder)hostedAppBuilder).Properties;
    ///<inheritdoc/>
    public IConfigurationManager Configuration => hostedAppBuilder.Configuration;
    ///<inheritdoc/>
    public IHostEnvironment Environment => hostedAppBuilder.Environment;
    ///<inheritdoc/>
    public ILoggingBuilder Logging => throw new NotSupportedException("Use ApplicationSetup.LogFactory...");
    ///<inheritdoc/>
    public IMetricsBuilder Metrics => hostedAppBuilder.Metrics;
    ///<inheritdoc/>
    public IServiceCollection Services => hostedAppBuilder.Services;

    ///<inheritdoc/>
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull
     => ((IHostApplicationBuilder)hostedAppBuilder).ConfigureContainer(factory, configure);

    /// <summary>Setup host</summary>
    protected virtual void setupHost(IHost host) {
      App.Setup= App.Setup with { ServiceProv= host.Services };
      App.AppLifetime.ApplicationStopped.Register(() => {
        log.LogCritical("Shutdown.\n\n");
        Serilog.Log.CloseAndFlush();
      });
    }

    /// <summary><see cref="IHostEnvironment"/></summary>
    protected IHostEnvironment? hostEnv;
    /// <summary><see cref="IHostEnvironment"/></summary>
    protected IHostEnvironment HostEnv {
      get {
        this.hostEnv??= Services.Where(sd => sd.ServiceType == typeof(IHostEnvironment))
                                .Select(sd => sd.ImplementationInstance as IHostEnvironment)
                                .Last() ?? throw new InvalidOperationException($"Missing {nameof(IHostEnvironment)}");
        return this.hostEnv;
      }
    }
    // protected override IHostEnvironment HostEnv {
    //   get {
    //     if (null == this.hostEnv) Host.ConfigureAppConfiguration((ctx, b) => this.hostEnv= ctx.HostingEnvironment);
    //     return this.hostEnv ?? throw new InvalidOperationException($"Missing {nameof(IHostEnvironment)}");
    //   }
    // }


  }
}
