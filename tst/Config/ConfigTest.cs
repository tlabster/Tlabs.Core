using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Config.Tests {

  public class ConfigTest {

    public class TestConfigurator : IConfigurator<ConfigTest> {
      public void AddTo(ConfigTest target, IConfiguration cfg) {
        ++target.appliedConfigCount;
      }
    }

    private readonly ITestOutputHelper tstout;
    private int appliedConfigCount= 0;

    public ConfigTest(ITestOutputHelper output) {
      this.tstout= output;
    }

    [Fact]
    public void UtilTest() {
      tstout.WriteLine("");
      tstout.WriteLine($"working dir: {App.ContentRoot}");
      tstout.WriteLine($"main entry path: {App.MainEntryPath}");

      var config= ConfigUtilsExtensions.LoadJson(
        "test-config",
        Path.Combine(App.ContentRoot, "rsc")
      ).Build();
      
      var configMap= config.ToDictionary();
      Assert.NotEmpty(configMap);
      Assert.Equal("test", configMap["rootProperty"]);

      foreach(var entry in configMap)
        tstout.WriteLine($"[{entry.Key}]= '{entry.Value}'");

      this.ApplyConfigurators(config, "tstSection");
      Assert.Equal(1, this.appliedConfigCount);

    }

  }

}