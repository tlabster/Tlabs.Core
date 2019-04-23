using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

  ///<summary>Middleware context used with a <see cref="IConfigurator{MiddlewareContext}"/>./>.</summary>
  public class MiddlewareContext {
    ///<summary>Hosting environment</summary>
    public IHostingEnvironment HostingEnv { get; set; }
    ///<summary>Application builder to be configured.</summary>
    public IApplicationBuilder AppBuilder { get; set; }
  }

  ///<summary>Configuration extensions.</summary>
  public static class ConfigUtilsExtensions {
    ///<summary>Apply section configurators to <typeparamref name="T"/>.</summary>
    public static T ApplyConfigurators<T>(this T target, IConfiguration config, string subSectionName= null) {
      IConfiguration section= (string.IsNullOrEmpty(subSectionName) ? null : config.GetSection(subSectionName)) ?? config;
      foreach(var conf in section.LoadObject<IConfigurator<T>>()) {
        conf.Object.AddTo(target, section.GetSection(conf.SectionName));
      }
      return target;
    }

    ///<summary>Convert <paramref name="config"/> into flattened dictionary.</summary>
    ///<param name="config">Configuration (section)</param>
    ///<param name="stripSectionPath">if true (default) and config is section, strips off the section path from keys</param>
    public static Dictionary<string, string> ToDictionary(this IConfiguration config, bool stripSectionPath = true) {
      var data= new Dictionary<string, string>();
      var section= stripSectionPath ? config as IConfigurationSection : null;
      convertToDictionary(config, data, section);
      return data;
    }

    private static void convertToDictionary(IConfiguration config, Dictionary<string, string> data, IConfigurationSection top = null) {
      foreach (var child in config.GetChildren()) {
        if (null == child.Value) {
          convertToDictionary(config.GetSection(child.Key), data, top);
          continue;
        }

        var key= top != null ? child.Path.Substring(top.Path.Length + 1) : child.Path;
        data[key]= child.Value;
      }
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

  }


  /// <summary>Base class to configure ist-self als enumerable of <typeparamref name="T"/>.</summary>
  public abstract class SelfEnumConfigurator<T> : IConfigurator<IServiceCollection> {
    ///<inherit/>
    public void AddTo(IServiceCollection services, IConfiguration cfg) {
      services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(T), GetType()));  //add self
    }
  }


  ///<summary>Empty <see cref="IConfiguration"/>.</summary>
  public class Empty : IConfiguration {
    ///<summary>Empty Configuration</summary>
    public static readonly IConfiguration Configuration= new Empty();
    ///<inherit/>
    public string this[string key] { get => null; set => throw new NotImplementedException(); }
    ///<inherit/>
    public IEnumerable<IConfigurationSection> GetChildren() => null;
    ///<inherit/>
    public IChangeToken GetReloadToken() => null;
    ///<inherit/>
    public IConfigurationSection GetSection(string key) => null;
  }
}