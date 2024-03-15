using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using Tlabs.Misc;

namespace Tlabs.Config {

  ///<summary>Empty <see cref="IConfiguration"/>.</summary>
  public class Empty : IConfigurationSection {
    private sealed class NoChgToken : IChangeToken {
      private sealed class Dsp : IDisposable { public void Dispose() { } }
      public bool HasChanged => false;
      public bool ActiveChangeCallbacks => false;
      public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => Singleton<Dsp>.Instance;
    }
    ///<summary>Empty Configuration</summary>
    public static readonly IConfigurationSection Configuration= new Empty();
    ///<inheritdoc/>
    public string? this[string key] { get => null; set => throw new NotImplementedException(); }
    ///<inheritdoc/>
    public string Key => String.Empty;
    ///<inheritdoc/>
    public string Path => String.Empty;
    ///<inheritdoc/>
    public string? Value { get => null; set => throw new NotImplementedException(); }
    ///<inheritdoc/>
    public IEnumerable<IConfigurationSection> GetChildren() => System.Linq.Enumerable.Empty<IConfigurationSection>();
    ///<inheritdoc/>
    public IChangeToken GetReloadToken() => Singleton<NoChgToken>.Instance;
    ///<inheritdoc/>
    public IConfigurationSection GetSection(string key) => Configuration;
  }
}