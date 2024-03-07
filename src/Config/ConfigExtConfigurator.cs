using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tlabs.Config {

  ///<summary>Json settings file extension configurator.</summary>
  public class JsonConfigExtConfigurator : IConfigurator<IConfigurationBuilder> {
    ///<summary>Json file config property.</summary>
    public const string JSON_FILE_CFG= "jsonFile";

    static readonly ILogger log= App.Logger<JsonConfigExtConfigurator>();
    readonly IReadOnlyDictionary<string, string> config= ImmutableDictionary<string, string>.Empty;

    ///<summary>Default ctor.</summary>
    public JsonConfigExtConfigurator() { }

    ///<summary>Ctor from <paramref name="config"/>.</summary>
    public JsonConfigExtConfigurator(IReadOnlyDictionary<string, string> config) {
      this.config= config;
    }

    ///<inheritdoc/>
    public void AddTo(IConfigurationBuilder cfgBuilder, IConfiguration cfg) {
      config.TryGetValue(JSON_FILE_CFG, out var jsonFile);
      if (string.IsNullOrEmpty(jsonFile)) {
        log.LogError("Missing config.{fn} property in {cfg}", JSON_FILE_CFG, cfg);
        return;
      }
      string cfgPath= jsonFile;
      if (!Path.IsPathRooted(jsonFile))
        cfgPath= Path.Combine(Path.GetDirectoryName(App.MainEntryPath) ??"", cfgPath);

      if (File.Exists(cfgPath)) {
        cfgBuilder.AddJsonFile(cfgPath);
        log.LogInformation("Config extension loaded from: {file}", jsonFile);
      }
    }
  }

  ///<summary>Environment config. values configurator.</summary>
  public class EnvConfigExtConfigurator : IConfigurator<IConfigurationBuilder> {
    ///<summary>Env. variable name prefix</summary>
    public const string ENV_PREFIX_CFG= "envPrefix";
    readonly IReadOnlyDictionary<string, string> config= ImmutableDictionary<string, string>.Empty;

    ///<summary>Default ctor.</summary>
    public EnvConfigExtConfigurator() { }

    ///<summary>Ctor from <paramref name="config"/>.</summary>
    public EnvConfigExtConfigurator(IReadOnlyDictionary<string, string> config) {
      this.config= config;
    }

    ///<inheritdoc/>
    public void AddTo(IConfigurationBuilder builder, IConfiguration cfg) {
      if (   !config.TryGetValue(ENV_PREFIX_CFG, out var prefix)
          || string.IsNullOrEmpty(prefix)) return;

      builder.AddEnvironmentVariables(prefix);
    }
  }

}