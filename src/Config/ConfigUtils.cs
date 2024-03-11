using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Primitives;

using Tlabs.Misc;

namespace Tlabs.Config {

  ///<summary>Base interface of a dynamically loaded configurator.</summary>
  public interface IConfigurator<T> {
    ///<summary>Method called to add a specific configuration to a <paramref name="target"/>.</summary>
    void AddTo(T target, IConfiguration cfg);
  }

  ///<summary>Sub-Section configurator.</summary>
  public class SubSectionConfigurator<T> : IConfigurator<T> {
    ///<inheritdoc/>
    public void AddTo(T target, IConfiguration cfg) {
      foreach (var conf in cfg.LoadConfigurationObjects<IConfigurator<T>>(excludeObjDesc: true)) {
        conf.Object.AddTo(target, cfg.GetSection(conf.SectionName));
      }
    }
  }

  ///<summary>Configuration extensions.</summary>
  public static class ConfigUtilsExtensions {
    ///<summary>Apply section configurators to <typeparamref name="T"/>.</summary>
    public static T ApplyConfigurators<T>(this T target, IConfiguration config, string? subSectionName= null) {
      IConfiguration section= (string.IsNullOrEmpty(subSectionName) ? null : config.GetSection(subSectionName)) ?? config;
      foreach(var conf in section.LoadConfigurationObjects<IConfigurator<T>>()) {
        conf.Object.AddTo(target, section.GetSection(conf.SectionName));
      }
      return target;
    }

    ///<summary>Convert <paramref name="config"/> into flattened dictionary.</summary>
    ///<param name="config">Configuration (section)</param>
    ///<param name="stripSectionPath">if true (default) and config is section, strips off the section path from keys</param>
    public static Dictionary<string, string?> ToDictionary(this IConfiguration config, bool stripSectionPath= true) {
      static void convertToDictionary(IConfiguration config, Dictionary<string, string?> data, IConfigurationSection? top= null) {
        foreach (var child in config.GetChildren()) {
          if (null == child.Value) {
            convertToDictionary(config.GetSection(child.Key), data, top);
            continue;
          }
          var key= top != null ? child.Path.Substring(top.Path.Length + 1) : child.Path;
          data[key]= child.Value;
        }
      }
      var data= new Dictionary<string, string?>();
      var section= stripSectionPath ? config as IConfigurationSection : null;
      convertToDictionary(config, data, section);
      return data;
    }

    ///<summary>Convert <paramref name="config"/> into nested dictionary.</summary>
    ///<param name="config">Configuration (section)</param>
    public static Dictionary<string, object> ToNestedDictionary(this IConfiguration config) {
      return config.GetChildren().ToDictionary(child => child.Key,
                                               child => (object?)child.Value ?? config.GetSection(child.Key).ToNestedDictionary());
    }

    ///<summary>Load <see cref="IConfigurationBuilder"/> form json file.</summary>
    public static IConfigurationBuilder LoadJson(string baseName, string path, string? env= null)
      => new ConfigurationBuilder().AddJsonConfig(baseName, path, env);

    ///<summary>Add JSON configuration to <paramref name="builder"/> <see cref="IConfigurationBuilder"/>.</summary>
    ///<remarks>JSON configuration is loaded from
    ///<para>- {path}/{basename}.json  (required)</para>
    ///<para>- {path}/{basename}.{resolved-ENV-variable}.json  (optional)</para>
    ///<example>
    ///   appsettings.Development.json
    ///   appsettings.Azure.json
    ///</example>
    ///</remarks>
    public static IConfigurationBuilder AddJsonConfig(this IConfigurationBuilder builder, string baseName, string path, string? env= null)
      => builder.SetBasePath(path)
             .AddJsonFile($"{baseName}.json", optional: false, reloadOnChange: true)
             .AddJsonFile($"{baseName}.{env ?? "__not_specified__"}.json", optional: true);


    ///<summary>Base settings file name.</summary>
    public const string DEFAULT_JSON_BASE_FILENAME= "appsettings";
    ///<summary>Add JSON configuration to <paramref name="builder"/> <see cref="IConfigurationBuilder"/>.</summary>
    ///<remarks>JSON configuration is loaded from
    ///<para>- {path}/{basename}.json  (required)</para>
    ///<para>- {path}/{basename}.{resolved-ENV-variable}.json  (optional)</para>
    ///</remarks>
    public static IConfigurationBuilder AddJsonConfig(this IConfigurationBuilder builder, string? baseJsonFile= null, string? env= null) {
      baseJsonFile??= DEFAULT_JSON_BASE_FILENAME;
      var jsonPath= App.ContentRoot;
      if (Path.IsPathRooted(baseJsonFile)) {
        jsonPath= Path.GetDirectoryName(baseJsonFile) ?? "";
        baseJsonFile= Path.GetFileNameWithoutExtension(baseJsonFile);
      }
      return builder.AddJsonConfig(baseJsonFile, jsonPath, env);
    }

    ///<summary>Config extension section.</summary>
    public const string XCFG_SECTION= "configExtensions";
    ///<summary>Add application configuration to <paramref name="builder"/> <see cref="IConfigurationBuilder"/>.</summary>
    ///<remarks>The application configuration is loaded from
    ///<para>- <paramref name="baseJsonFile"/> (<see cref="AddJsonConfig(IConfigurationBuilder, string?, string?)"/>) or none if <paramref name="baseJsonFile"/> == "" </para>
    ///<para>- environment variables with <paramref name="envVarPrefix"/>  (optional)</para>
    ///<para>- <paramref name="cmdArgs"/>  (optional)</para>
    ///</remarks>
    public static IConfigurationBuilder AddApplicationConfig(this IConfigurationBuilder builder, string? baseJsonFile= null, string? env= null, string[]? cmdArgs= null, string? envVarPrefix= null) {
      if (string.Empty != baseJsonFile) {
        builder.AddJsonConfig(baseJsonFile, env)
               .ApplyConfigurators(builder.Build(), XCFG_SECTION);
      }
      if (!string.IsNullOrEmpty(envVarPrefix)) builder.AddEnvironmentVariables(envVarPrefix);
      if (null != cmdArgs) builder.AddCommandLine(cmdArgs);
      return builder;
    }


    ///<summary>Convert section into new <see cref="IConfigurationBuilder"/>.</summary>
    public static IConfigurationBuilder ToConfigurationBuilder(this IConfigurationSection section) {
      var builder= new ConfigurationBuilder().AddInMemoryCollection(section.ToDictionary());
      return builder;
    }

    ///<summary>Convert dictionary into <see cref="IConfiguration"/>.</summary>
    public static IConfiguration ToConfiguration(this IReadOnlyDictionary<string, string?> dict) {
      var builder= new ConfigurationBuilder().AddInMemoryCollection(dict);
      return builder.Build();
    }

  }


  /// <summary>Base class to configure it-self als enumerable of <typeparamref name="T"/>.</summary>
  public abstract class SelfEnumConfigurator<T> : IConfigurator<IServiceCollection> {
    ///<inheritdoc/>
    public void AddTo(IServiceCollection services, IConfiguration cfg) {
      services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(T), GetType()));  //add self
    }
  }

}