using System;
using System.IO;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tlabs.Config;

using Xunit;

namespace Tlabs.Tests {

  public class AppGlobalTest {

    [Fact]
    public void AppTest() {
      Assert.NotNull(App.Setup);
      Assert.NotEmpty(App.Setup.ContentRoot);
      Assert.NotNull(App.Setup.ConfigMngr);
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
    public void AppStartupTest() {
      var appFile= Path.Combine(App.Setup.ContentRoot, "appsettings.json");
      File.Delete(appFile);
      File.WriteAllText(appFile, $"{{ \"{ApplicationStartup.DFLT_HOST_SECTION}\": {{ }} }}");
      var dfltSvcProv= App.ServiceProv;
      var dfltAppLft= App.AppLifetime;

      var hostBuilder= ApplicationStartup.CreateAppHostBuilder();
      Assert.NotNull(hostBuilder);

      using var host= hostBuilder.Build();
      Assert.NotNull(host);
      Assert.NotEmpty(App.Settings.GetChildren());
      Assert.Equal(ApplicationStartup.DFLT_HOST_SECTION, App.Settings.GetChildren().Single().Key);

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

  }
}
