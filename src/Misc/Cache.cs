using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Tlabs.Misc {

  ///<summary>Cache.</summary>
  public interface ICache<K, T> where K : notnull where T : class {

    ///<summary>Get or set value with <paramref name="key"/>.</summary>
    ///<returns>Cached entry or null if no value cached for <paramref name="key"/>.</returns>
    T? this[K key] { get; set; }

    ///<summary>Gets an already cached value for the given <paramref name="key"/> or if no cached value exists adds the value returned from <paramref name="getValue"/>.</summary>
    ///<returns>The value for the <paramref name="key"/> in the cache or the value returned from <paramref name="getValue"/>.</returns>
    T? this[K key, Func<T> getValue] { get; }

    ///<summary>Evict entry with <paramref name="key"/>.</summary>
    ///<returns>Evicted entry or null if no value cached for <paramref name="key"/>.</returns>
    T? Evict(K key);

    ///<summary>Cached entries.</summary>
    ///<returns>Snapshot of the currently cached entries.</returns>
    IEnumerable<KeyValuePair<K, T>> Entries { get; }
  }

  ///<summary>Basic cache supporting concurrent access with balanced read/write locking.</summary>
  public class BasicCache<K, T> : IDisposable, ICache<K, T> where K : notnull where T : class {
    readonly Dictionary<K, T> cache;
    readonly ReaderWriterLockSlim lck= new();

    ///<summary>Default ctor.</summary>
    public BasicCache() {
      this.cache= new Dictionary<K, T>();
    }

    ///<summary>Ctor to initialize from <paramref name="init"/>.</summary>
    public BasicCache(IDictionary<K, T> init) {
      this.cache= new Dictionary<K, T>(init);
    }

    ///<inheritdoc/>
    public T? this[K key] {
      get {
        lck.EnterReadLock();
        try {
          cache.TryGetValue(key, out var val);
          return val!;
        }
        finally { lck.ExitReadLock(); }
      }
      set {
        if (null == value) return;
        lck.EnterWriteLock();
        try {
          cache[key]= value;
        }
        finally { lck.ExitWriteLock(); }
      }
    }

    ///<inheritdoc/>
    public T this[K key, Func<T> getValue] {
      get {
        lck.EnterUpgradeableReadLock();
        try {
          if (cache.TryGetValue(key, out var val)) return val;
          return this[key]= getValue();
        }
        finally { lck.ExitUpgradeableReadLock(); }
      }
    }

    ///<inheritdoc/>
    public T? Evict(K key) {
      lck.EnterUpgradeableReadLock();
      try {
        if (cache.TryGetValue(key, out var val)) {
          lck.EnterWriteLock();
          try {
            cache.Remove(key);
          }
          finally { lck.ExitWriteLock(); }
        }
        return val;
      }
      finally { lck.ExitUpgradeableReadLock(); }
    }

    ///<inheritdoc/>
    public IEnumerable<KeyValuePair<K, T>> Entries { get {
      lck.EnterReadLock();
      try {
        return new List<KeyValuePair<K, T>>(cache);
      }
      finally { lck.ExitReadLock(); }
    }}

    ///<inheritdoc/>
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    ///<summary>Do dispose</summary>
    protected virtual void Dispose(bool disposing) {
      if (disposing) lck.Dispose();
    }
  }


  ///<summary>Cache optimized for concurrent lock free lookups (read).</summary>
  ///<remarks>Concurrent writes are supported but incur more overhead...</remarks>
  public class LookupCache<K, T> : ICache<K, T> where K : notnull where T : class {
    readonly ConcurrentDictionary<K, T> cache;

    ///<summary>Default ctor.</summary>
    public LookupCache() => this.cache= new ConcurrentDictionary<K, T>();

    ///<summary>Default ctor.</summary>
    public LookupCache(IEnumerable<KeyValuePair<K, T>> data) => this.cache= new ConcurrentDictionary<K, T>(data);
    ///<inheritdoc/>
    public T? this[K key] {
      get {
        cache.TryGetValue(key, out var val);
        return val;
      }
      set { if (null != value) cache[key]= value; }
    }

    ///<inheritdoc/>
    public T? this[K key, Func<T> getValue] => cache.GetOrAdd(key, (k) => getValue());

    ///<inheritdoc/>
    public IEnumerable<KeyValuePair<K, T>> Entries => cache;

    ///<inheritdoc/>
    public T? Evict(K key) {
      cache.TryRemove(key, out var removedVal);
      return removedVal;
    }
  }

}