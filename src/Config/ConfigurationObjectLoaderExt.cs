using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tlabs.Config {

  ///<summary>Extension class for loading <see cref="IConfigurator{T}"/>(s) from a <see cref="IConfigurationSection"/>.</summary>
  public static class ConfigurationObjectLoaderExt {
    internal class ObjectDescriptor {
      public int ord { get; set; }
      public string type { get; set; }
      public Dictionary<string, string> config { get; set; }
    }

    ///<summary>Configuration object with section key.</summary>
    public struct CfgObject<T> {
      ///<summary>Section key.</summary>
      public string SectionName;
      ///<summary>Configuration object instance.</summary>
      public T Object;
    }

    ///<summary>Returns an enumeration of <see cref="CfgObject{T}"/>(s) with <see cref="CfgObject{T}.Object"/> instances loaded from a <see cref="IConfiguration"/>.</summary>
    public static IEnumerable<CfgObject<T>> LoadObject<T>(this IConfiguration cfg) where T : class {
      var secName= (cfg as IConfigurationSection)?.Key ?? "?";
      var typeName= typeof(T).Name;
      var types= new Dictionary<string, ObjectDescriptor>();
      cfg.Bind(types);

      foreach (var tpair in types.OrderBy(p => p.Value?.ord ?? 0)) {
        ObjectDescriptor objDsc= tpair.Value;
        if (null == objDsc || String.IsNullOrEmpty(objDsc.type)) {
          // do not use any logger, since it could be to early for logger being initialized
          Console.WriteLine($"Invalid {nameof(ObjectDescriptor)} - {{0}} instance NOT LOADED (from section {{1}}.{{2}}).", typeName, secName, tpair.Key ?? "?");
          continue;
        }
        var configDesc= $"{secName}:{tpair.Key ?? "?"}:type: {objDsc.type}";
        Type tp= Misc.Safe.LoadType(objDsc.type, configDesc);
        T obj= null;
        try {
          try { // frist from ctor taking a config dictionary
            obj= (T)Activator.CreateInstance(tp, new object[] {objDsc.config});
          }
          catch (MissingMemberException) {
            obj= (T)Activator.CreateInstance(tp); //try with default ctor
          }
        }
        catch (Exception e) { throw new AppConfigException($"Failed to create {typeName} instance for {configDesc}", e); }
        if (null != obj)
          yield return new CfgObject<T> {
            SectionName= tpair.Key,
            Object= obj
          };
      }
    }

  }

}