﻿using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Primitives;

namespace Tlabs.Config {

  ///<summary>Base interface of a dynamically loaded configurator.</summary>
  public interface IConfigurator<T> {
    ///<summary>Method called to add a specific configuration to a <paramref name="target"/>.</summary>
    void AddTo(T target, IConfiguration cfg);
  }

  ///<summary>Configuration extensions.</summary>
  public static class ConfigUtilsExtensions {
    ///<summary>Apply section configurators to <typeparamref name="T"/>.</summary>
    public static T ApplyConfigurators<T>(this T target, IConfiguration config, string subSectionName= null) {
      IConfiguration section= (string.IsNullOrEmpty(subSectionName) ? null : config.GetSection(subSectionName)) ?? config;
      foreach(var conf in section.LoadConfigurationObjects<IConfigurator<T>>()) {
        conf.Object.AddTo(target, section.GetSection(conf.SectionName));
      }
      return target;
    }

    ///<summary>Convert <paramref name="config"/> into flattened dictionary.</summary>
    ///<param name="config">Configuration (section)</param>
    ///<param name="stripSectionPath">if true (default) and config is section, strips off the section path from keys</param>
    public static Dictionary<string, string> ToDictionary(this IConfiguration config, bool stripSectionPath= true) {
      static void convertToDictionary(IConfiguration config, Dictionary<string, string> data, IConfigurationSection top = null) {
        foreach (var child in config.GetChildren()) {
          if (null == child.Value) {
            convertToDictionary(config.GetSection(child.Key), data, top);
            continue;
          }
          var key = top != null ? child.Path.Substring(top.Path.Length + 1) : child.Path;
          data[key]= child.Value;
        }
      }
      var data= new Dictionary<string, string>();
      var section= stripSectionPath ? config as IConfigurationSection : null;
      convertToDictionary(config, data, section);
      return data;
    }

    ///<summary>Convert <paramref name="config"/> into nested dictionary.</summary>
    ///<param name="config">Configuration (section)</param>
    public static Dictionary<string, object> ToNestedDictionary(this IConfiguration config) {
      return config.GetChildren().ToDictionary(child => child.Key,
                                               child => (object)   (null == child.Value
                                                                 ? config.GetSection(child.Key).ToNestedDictionary()
                                                                 : child.Value));
    }

    ///<summary>Load <see cref="IConfigurationBuilder"/> form json file.</summary>
    public static IConfigurationBuilder LoadJson(string baseName, string path, string env = "") {
      var builder= new ConfigurationBuilder()
        .SetBasePath(path)
        .AddJsonFile($"{baseName}.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"{baseName}.{env ?? "__not_specified__"}.json", optional: true);
      return builder;
    }

    ///<summary>Convert section into new <see cref="IConfigurationBuilder"/>.</summary>
    public static IConfigurationBuilder ToConfigurationBuilder(this IConfigurationSection section) {
      var builder= new ConfigurationBuilder()
        .AddInMemoryCollection(section.ToDictionary());
      return builder;
    }

    ///<summary>Convert dictionary into <see cref="IConfiguration"/>.</summary>
    public static IConfiguration ToConfiguration(this IReadOnlyDictionary<string, string> dict) {
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


  ///<summary>Empty <see cref="IConfiguration"/>.</summary>
  public class Empty : IConfigurationSection {
    ///<summary>Empty Configuration</summary>
    public static readonly IConfigurationSection Configuration= new Empty();
    ///<inheritdoc/>
    public string this[string key] { get => null; set => throw new NotImplementedException(); }
    ///<inheritdoc/>
    public string Key => String.Empty;
    ///<inheritdoc/>
    public string Path => String.Empty;
    ///<inheritdoc/>
    public string Value { get => null; set => throw new NotImplementedException(); }
    ///<inheritdoc/>
    public IEnumerable<IConfigurationSection> GetChildren() => System.Linq.Enumerable.Empty<IConfigurationSection>();
    ///<inheritdoc/>
    public IChangeToken GetReloadToken() => null;
    ///<inheritdoc/>
    public IConfigurationSection GetSection(string key) => Configuration;
  }
}