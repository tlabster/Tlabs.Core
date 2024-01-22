using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tlabs.Config {

  ///<summary>Configurator to add additional assembly path(s) for config target type <typeparamref name="T"/>.</summary>
  public class AssemblyPathConfigurator<T> : IConfigurator<T> {
    readonly object sync= new();
    readonly ILogger log;
    readonly string basePath;
    string? cfgPath;

    ///<summary>Default ctor.</summary>
    public AssemblyPathConfigurator() : this(null) { }
    ///<summary>Ctor from <paramref name="config"/> dictionary.</summary>
    public AssemblyPathConfigurator(IDictionary<string, string>? config) {
      this.log= App.Logger<AssemblyPathConfigurator<T>>();
      var cfg= config ?? new Dictionary<string, string>();
      this.basePath= Path.GetDirectoryName(App.MainEntryPath) ?? "";
      log.LogDebug("Assembly base-path: {basePath}", basePath);
      if (!cfg.TryGetValue("path", out cfgPath)) return;   //no assembly path to be set
    }
    ///<summary>Add additional path(s) to <paramref name="target"/>.</summary>
    public void AddTo(T target, IConfiguration cfg) {
      ExtendAsmPath();
    }

    ///<summary>Extend assembly path(s) as configured.</summary>
    public void ExtendAsmPath() {
      if (null != cfgPath) lock(sync) if (null != cfgPath) {

        var paths= cfgPath.Split(';');
        cfgPath= null;
        AssemblyLoadContext.Default.Resolving+= (ctx, asmName) => {
          Assembly? asm= null;
          if (null != asmName.Name) foreach (var path in paths) {
            var asmPath= Path.Combine(basePath, path, asmName.Name) + ".dll";
            if (File.Exists(asmPath)) try {
              log.LogInformation("Resolving extended assembly: {asmName}", asmName.FullName);
              asm= ctx.LoadFromAssemblyPath(Path.Combine(basePath, asmName.Name + ".dll"));
              log.LogDebug(" {asmName} loaded from: {asmPath}", asmName.FullName, asmPath);
              break;
            }
            catch (Exception e ) when(Misc.Safe.NoDisastrousCondition(e)) {
              log.LogWarning(0, e, "Failed to load assembly: '{asmPath}'", asmPath);
            }
          }
          return asm;
        };
      }
    }

  }

  ///<summary>Configurator to add additional assembly path(s).</summary>
  public class AssemblyPathConfigurator : AssemblyPathConfigurator<IHostBuilder> { }

}