using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tlabs.Config {

  ///<summary>Json settings file extension configurator.</summary>
  public class JsonConfigExtension : IConfigurator<IConfigurationBuilder> {
    ///<summary>Json file config property.</summary>
    public const string JSON_FILE_CFG= "jsonFile";

    static readonly ILogger log= App.Logger<JsonConfigExtension>();
    readonly IDictionary<string, string> config;

    ///<summary>Default ctor.</summary>
    public JsonConfigExtension() { }

    ///<summary>Ctor from <paramref name="config"/>.</summary>
    public JsonConfigExtension(IDictionary<string, string> config) {
      this.config= config ?? new Dictionary<string, string>();
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
        cfgPath= Path.Combine(Path.GetDirectoryName(App.MainEntryPath), cfgPath);

      if (File.Exists(cfgPath)) {
        cfgBuilder.AddJsonFile(cfgPath);
        log.LogInformation("Config extension loaded from: {file}", jsonFile);
      }
    }

  }
}
