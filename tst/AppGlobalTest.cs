using System;
using System.IO;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tlabs.Config;

using Xunit;

namespace Tlabs.Tests {

  [Collection("App.Setup")]
  public class AppGlobalTest : IDisposable {
    static readonly object sync= new();
    [Fact]
    public void AppTest() {
      App.Setup= ApplicationSetup.Default;

      Assert.NotNull(App.Setup);
      Assert.NotEmpty(App.Setup.ContentRoot);
      Assert.NotNull(App.Setup.Configuration);
      Assert.NotNull(App.Setup.LogFactory);
      Assert.NotNull(App.Setup.ServiceProv);
      Assert.NotNull(App.Setup.TimeInfo);

      Assert.Empty(App.Settings.GetChildren());
      Assert.Empty(App.Settings.ToDictionary());
      Assert.Null(App.ServiceProv.GetService(typeof(IServiceScopeFactory)));
      Assert.NotNull(App.AppLifetime);
      Assert.Throws<NotImplementedException>(() => App.AppLifetime.ApplicationStopped.IsCancellationRequested);

      var log= App.Logger<AppGlobalTest>();
      Assert.NotNull(log);
      log.LogTrace("test");
    }

    [Fact]
    public void CreateHostAppBuilderTest() {
      App.Setup= ApplicationSetup.Default;

      var appFile= Path.Combine(App.Setup.ContentRoot, "appsettings.json");
      File.Delete(appFile);
      File.WriteAllText(appFile, $"{{ \"{ApplicationStartup.DFLT_HOST_SECTION}\": {{ \"hostValue\": \"hostValue\"}} }}");
      var dfltSvcProv= App.ServiceProv;
      var dfltAppLft= App.AppLifetime;
      var dotNetVarName= $"{ApplicationStartup.ENV_DOTNET_PFX}envValue";
      Environment.SetEnvironmentVariable(dotNetVarName, "envValue");

      var hostBuilder= ApplicationStartup.CreateAppHostBuilder();
      Assert.NotNull(hostBuilder);

      hostBuilder.ConfigureHostConfiguration(cfgBuilder => {
        var cfg= cfgBuilder.Build();
        Assert.Equal("hostValue", cfg.GetValue<string>("hostValue"));
        Assert.Equal("envValue", cfg.GetValue<string>("envValue"));
      });
      using var host= hostBuilder.Build();
      Assert.NotNull(host);

      Assert.NotEqual(dfltSvcProv, App.ServiceProv);
      Assert.NotEqual(dfltAppLft, App.AppLifetime);
      Assert.NotNull(App.ServiceProv.GetService(typeof(IServiceScopeFactory)));

#if START_TEST
      var stopCnt=0;
      App.AppLifetime.ApplicationStopped.Register(()=> ++stopCnt);

      await host.StartAsync();
      host.Run();
      App.AppLifetime.StopApplication();    //this seems to cancel all worker tasks - and breaks other async tests
      await host.StopAsync();
      (App.ServiceProv as IDisposable)?.Dispose();
      Assert.Equal(1, stopCnt);
#endif
    }


    [Fact]
    public void HostedAppBuilderTest() {
      App.Setup= ApplicationSetup.Default;

      var appFile= Path.Combine(App.Setup.ContentRoot, "appsettings.json");
      File.Delete(appFile);
      File.WriteAllText(appFile, $"{{ \"{HostedAppBuilder.DFLT_HOST_SECTION}\": {{ \"hostValue\": \"hostValue\"}} }}");
      var dfltSvcProv= App.ServiceProv;
      var dfltAppLft= App.AppLifetime;
      var dotNetVarName= $"{ApplicationStartup.ENV_DOTNET_PFX}envValue";
      Environment.SetEnvironmentVariable(dotNetVarName, "envValue");

      var hostBuilder= new HostedAppBuilder();
      Assert.NotNull(hostBuilder);

      var cfg= hostBuilder.Configuration.Build();
      Assert.Equal("hostValue", cfg.GetValue<string>("hostValue"));
      Assert.Equal("envValue", cfg.GetValue<string>("envValue"));

      using var host= hostBuilder.Build();
      Assert.NotNull(host);

      Assert.NotEqual(dfltSvcProv, App.ServiceProv);
      Assert.NotEqual(dfltAppLft, App.AppLifetime);
      Assert.NotNull(App.ServiceProv.GetService(typeof(IServiceScopeFactory)));
    }


    public void Dispose() => App.Setup= ApplicationSetup.Default;

  }
}
