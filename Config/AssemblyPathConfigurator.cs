using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tlabs.Config {

  ///<summary>Configurator to add additional assembly path(s).</summary>
  public class AssemblyPathConfigurator : IConfigurator<IWebHostBuilder> {
    private readonly ILogger log;
    private readonly string basePath;
    private string cfgPath;
    private string[] paths;

    ///<summary>Default ctor.</summary>
    public AssemblyPathConfigurator() : this(null) { }
    ///<summary>Ctor from <paramref name="config"/> dictionary.</summary>
    public AssemblyPathConfigurator(IDictionary<string, string> config) {
      this.log= App.Logger<AssemblyPathConfigurator>();
      var cfg= config ?? new Dictionary<string, string>();
      this.basePath= Path.GetDirectoryName(App.MainEntryPath);
      log.LogDebug("Assembly base-path: {basePath}", basePath);
      if (!cfg.TryGetValue("path", out cfgPath)) return;   //no assembly path to be set
    }
    ///<summary>Add additional path(s) to <paramref name="target"/>.</summary>
    public void AddTo(IWebHostBuilder target, IConfiguration cfg) {
      ExtendAsmPath();
    }

    ///<summary>Extend assembly path(s) as configured.</summary>
    public void ExtendAsmPath() {
      if (null != cfgPath) lock(cfgPath) if (null != cfgPath) {
      
        this.paths= cfgPath.Split(';');
        cfgPath= null;
        AssemblyLoadContext.Default.Resolving+= (ctx, asmName) => {
          log.LogInformation("Resolving extended assembly: {asmName}", asmName.FullName);
          Assembly asm= null;
          foreach (var path in this.paths) {
            var asmPath= Path.Combine(basePath, path, asmName.Name) + ".dll";
            if (File.Exists(asmPath)) try {
              asm= ctx.LoadFromAssemblyPath(Path.Combine(basePath, asmName.Name + ".dll"));
              log.LogDebug(" {asmName} loaded from: {asmPath}", asmName.FullName, asmPath);
              break;
            }
            catch (Exception e ) when(Misc.Safe.NoDisastrousCondition(e)) {
              log.LogWarning(0, e, $"Failed to load assembly: '{asmPath}'");
            }
          }
          return asm;
        };
      }
      
    }
  }


}