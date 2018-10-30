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

    ///<summary>Returns an enumeration of <see cref="IConfigurator{T}"/>(s) instances loaded from a <see cref="IConfiguration"/>.</summary>
    public static IEnumerable<T> LoadObject<T>(this IConfiguration cfg) where T : class {
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

        var qualifiedTypeName= objDsc.type ?? "?";
        var parts= qualifiedTypeName.Split('|');

        var configDesc= $"{secName}:{tpair.Key ?? "?"}:type: ";
        Type tp= loadPossiblyGenericType(parts, configDesc);
        T obj= null;
        try {
          try { // frist from ctor taking a config dictionary
            obj= (T)Activator.CreateInstance(tp, new object[] {objDsc.config});
          }
          catch (MissingMemberException) {
            obj= (T)Activator.CreateInstance(tp); //try with default ctor
          }
        }
        catch (Exception e) { throw new AppConfigException($"Failed to create {typeName} instance of {configDesc}{qualifiedTypeName}", e); }
        if (null != obj)
          yield return obj;
      }
    }

    private static Type loadPossiblyGenericType(string[] typeNameParts, string configDesc) {
      if (1 == typeNameParts.Length)  //simple non generic type?
        return Misc.Safe.LoadType(typeNameParts[0].Trim(), configDesc + typeNameParts[0]);

      var types= new Type[typeNameParts.Length];
      for (int l= 0; l < types.Length; ++l)
        types[l]= Misc.Safe.LoadType(typeNameParts[1].Trim(), configDesc + typeNameParts[1]);
      try {
        return types[0].MakeGenericType(types.Skip(1).ToArray());
      }
      catch (ArgumentNullException e) {
        throw new AppConfigException("Invalid generic type parameters in " + configDesc + typeNameParts[0], e);
      }
    }

  }

}