using System;
using System.Net.Http;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;


namespace Tlabs.Config {


  ///<summary>Configures an api client.</summary>
  public class ApiClientConfigurator : IConfigurator<IServiceCollection> {
    #pragma warning disable IDE0052 //keep fields for possible future use
    readonly IDictionary<string, string> config;
    // readonly ILogger log= App.Logger<ApiClientConfigurator>();

    ///<summary>Default ctor.</summary>
    public ApiClientConfigurator() : this(null) { }

    ///<summary>Ctor from <paramref name="config"/>.</summary>
    public ApiClientConfigurator(IDictionary<string, string>? config) {
      this.config= config ?? new Dictionary<string, string>();
    }

    ///<inheritdoc/>
    public void AddTo(IServiceCollection services, IConfiguration cfg) {
      //***TODO: add support for options */
      services.AddApiClient();
    }
  }

  ///<summary><see cref="IServiceCollection"/> extensions>.</summary>
  public static class ApiClientServiceExtension {
    ///<summary>Add a <see cref="HttpClient"/> configured as an api client>.</summary>
    public static IServiceCollection AddApiClient(this IServiceCollection services) {
      services.TryAddSingleton<HttpMessageHandler, Client.ApiClientHandler>();
      services.TryAddSingleton<HttpClient>();

      return services;
    }
  }
}