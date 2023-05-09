using System.Linq;
using System.IO;
using System.Collections.Generic;

using Tlabs.Misc;
using Tlabs.Sys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Config.Tests {

  public class ConfigTest : IClassFixture<ConfigTest.CfgContext> {

    public class CfgContext : IConfigurationRoot {
      public IConfigurationRoot Config { get; }

      public IEnumerable<IConfigurationProvider> Providers => throw new System.NotImplementedException();

      public string this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

      public CfgContext() {
        this.Config= ConfigUtilsExtensions.LoadJson(
          "test-config",
          Path.Combine(App.ContentRoot, "rsc")
        ).Build();
      }

      public void Reload() => throw new System.NotImplementedException();

      public IConfigurationSection GetSection(string key) => Config.GetSection(key);

      public IEnumerable<IConfigurationSection> GetChildren() => Config.GetChildren();

      public IChangeToken GetReloadToken() => throw new System.NotImplementedException();
    }

    public class TestConfigurator : IConfigurator<ConfigTest> {
      public void AddTo(ConfigTest target, IConfiguration cfg) {
        ++target.appliedConfigCount;
      }
    }

    public class TstGenric<A> : IConfigurator<ConfigTest> {
      public void AddTo(ConfigTest target, IConfiguration cfg) {
        ++target.appliedConfigCount;
      }
    }

    private int appliedConfigCount= 0;

    private readonly ITestOutputHelper tstout;
    private readonly ConfigTest.CfgContext config;

    public ConfigTest(ConfigTest.CfgContext config, ITestOutputHelper output) {
      this.config= config;
      this.tstout= output;
    }

    [Fact]
    public void UtilTest() {
      tstout.WriteLine("");
      tstout.WriteLine($"working dir: {App.ContentRoot}");
      tstout.WriteLine($"main entry path: {App.MainEntryPath}");

      var configMap= config.ToDictionary();
      Assert.NotEmpty(configMap);
      Assert.Equal("test", configMap["rootProperty"]);

      foreach(var entry in configMap)
        tstout.WriteLine($"[{entry.Key}]= '{entry.Value}'");

      configMap= config.GetSection("webHosting").ToDictionary();
      Assert.False(configMap.First().Key.StartsWith("webHosting")); //section key is stripped off

      this.ApplyConfigurators(config, "tstSection");
      Assert.Equal(2, this.appliedConfigCount);

      var configDict= config.ToNestedDictionary();
      Assert.IsType<Dictionary<string, object>>(configDict["webHosting"]);
      Assert.True(configDict.TryResolveValue("webHosting.configurator.assemblyPath.options.2", out var val, out var _)); //arrays become dictionaries keyed by index
      Assert.Equal("3", val.ToString());

    }

    [Fact]
    public void ConfigBindTest() {
      tstout.WriteLine("");
      var sysCmds= config.GetSection("sysCommands").Get<Dictionary<string, SysCmdTemplates>>();
      Assert.NotEmpty(sysCmds);
      Assert.Empty(sysCmds["LINUX"].CmdLines["hello"].WrkDir);
      Assert.NotEmpty(sysCmds["WINDOWS"].CmdLines["all"].WrkDir);
      tstout.WriteLine("");
    }

    [Fact]
    public void OSPlatformTest() {
      tstout.WriteLine("");
      tstout.WriteLine("OSPlatformTest:");
      tstout.WriteLine($"Linux: {System.Runtime.InteropServices.OSPlatform.Linux}");
      tstout.WriteLine($"Windows: {System.Runtime.InteropServices.OSPlatform.Windows}");
      tstout.WriteLine($"OsX: {System.Runtime.InteropServices.OSPlatform.OSX}");
      tstout.WriteLine($"FreeBSD: {System.Runtime.InteropServices.OSPlatform.FreeBSD}");
      tstout.WriteLine(".");
    }
  }

}