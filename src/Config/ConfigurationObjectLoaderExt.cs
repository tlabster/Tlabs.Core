using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Tlabs.Config {

  ///<summary>Extension class for loading <see cref="IConfigurator{T}"/>(s) from a <see cref="IConfigurationSection"/>.</summary>
  public static class ConfigurationObjectLoaderExt {
    internal class ObjectDescriptor {
      public int ord { get; set; }
      public string? type { get; set; }
      public Dictionary<string, string>? config { get; set; }
    }
    static string[] objDescFIELDS= typeof(ObjectDescriptor).GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).Select(fi => fi.Name).ToArray();

    ///<summary>Configuration object with section key.</summary>
    public struct CfgObject<T> {
      ///<summary>Section key.</summary>
      public string SectionName;
      ///<summary>Configuration object instance.</summary>
      public T Object;
    }

    ///<summary>Returns an enumeration of <see cref="CfgObject{T}"/>(s) with <see cref="CfgObject{T}.Object"/> instances loaded from a <see cref="IConfiguration"/>.</summary>
    public static IEnumerable<CfgObject<T>> LoadConfigurationObjects<T>(this IConfiguration cfg, bool excludeObjDesc= false) where T : class {
      var secName= (cfg as IConfigurationSection)?.Key ?? "?";
      var typeName= typeof(T).Name;
      var dict= new Dictionary<string, ObjectDescriptor>();
      cfg.Bind(dict);
      var objDcs=   excludeObjDesc
                  ? dict.Where(p => !objDescFIELDS.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
                  : (IEnumerable<KeyValuePair<string, ObjectDescriptor>>)dict;
      foreach (var tpair in objDcs.OrderBy(p => p.Value?.ord ?? 100)) {
        ObjectDescriptor objDsc= tpair.Value;
        if (null == objDsc || String.IsNullOrEmpty(objDsc.type)) {
          // do not use any logger, since it could be to early for logger being initialized
          Console.WriteLine($"Invalid {nameof(ObjectDescriptor)} - {{0}} instance NOT LOADED (from section {{1}}.{{2}}).", typeName, secName, tpair.Key ?? "?");
          continue;
        }
        var configDesc= $"{secName}:{tpair.Key ?? "?"}:type: {objDsc.type}";
        Type tp= Misc.Safe.LoadType(objDsc.type, configDesc);
        T? obj= null;
        try {
          try { // frist from ctor taking a config dictionary
            obj= (T?)Activator.CreateInstance(tp, new object?[] {objDsc.config});
          }
          catch (MissingMemberException) {
            obj= (T?)Activator.CreateInstance(tp); //try with default ctor
          }
        }
        catch (Exception e) { throw new AppConfigException($"Failed to create {typeName} instance for {configDesc}", e); }
        if (null != obj)
          yield return new CfgObject<T> {
            SectionName= tpair.Key!,
            Object= obj
          };
      }
    }

    ///<summary>Returns an enumeration of <see cref="CfgObject{T}"/>(s) with <see cref="CfgObject{T}.Object"/> instances loaded from a <see cref="IConfiguration"/>.</summary>
    [Obsolete("Use LoadConfigurationObjects<T>()", false)]
    public static IEnumerable<CfgObject<T>> LoadObject<T>(this IConfiguration cfg) where T : class => LoadConfigurationObjects<T>(cfg);

  }}