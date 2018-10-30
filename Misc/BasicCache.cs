using System;
using System.Collections.Generic;

namespace Tlabs.Misc {

  ///<summary>Simple lookup cache.</summary>
  public class BasicCache<K, T> {
    private Dictionary<K, T> cache= new Dictionary<K, T>();

    ///<summary>Get or set value with <paramref name="key"/>.</summary>
    ///<returns>Cached entry or null if no value cached for <paramref name="key"/>.</returns>
    public T this[K key] {
      get {
        T val;
        lock (cache) {
          cache.TryGetValue(key, out val);
          return val;
        }
      }
      set {
        lock (cache)
          cache[key]= value;
      }
    }

    ///<summary>Gets an already cached value for the given <paramref name="key"/> or adds <paramref name="newVal"/> if no cached value exists.</summary>
    ///<returns>The value for the <paramref name="key"/> in the cache or <paramref name="newVal"/>.</returns>
    public T this[K key, T newVal] {
      get {
        T val;
        lock (cache) {
          if (cache.TryGetValue(key, out val)) return val;
          return cache[key]= newVal;
        }
      }
    }

    ///<summary>Gets an already cached value for the given <paramref name="key"/> or adds or the value returned from <paramref name="getValue"/> if no cached value exists.</summary>
    ///<returns>The value for the <paramref name="key"/> in the cache or the value returned from <paramref name="getValue"/>.</returns>
    public T this[K key, Func<T> getValue] {
      get {
        T val;
        lock (cache) {
          if (cache.TryGetValue(key, out val)) return val;
          return cache[key]= getValue();
        }
      }
    }

    ///<summary>Evict entry with <paramref name="key"/>.</summary>
    ///<returns>Evicted entry or null if no value cached for <paramref name="key"/>.</returns>
    public T Evict(K key) {
      T val;
      lock (cache) {
        if (cache.TryGetValue(key, out val))
          cache.Remove(key);
        return val;
      }
    }
  }
}