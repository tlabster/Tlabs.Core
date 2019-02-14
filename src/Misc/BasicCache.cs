using System;
using System.Collections.Generic;
using System.Threading;

namespace Tlabs.Misc {

  ///<summary>Simple lookup cache.</summary>
  public class BasicCache<K, T> {
    private Dictionary<K, T> cache= new Dictionary<K, T>();
    private ReaderWriterLockSlim lck= new ReaderWriterLockSlim();

    ///<summary>Get or set value with <paramref name="key"/>.</summary>
    ///<returns>Cached entry or null if no value cached for <paramref name="key"/>.</returns>
    public T this[K key] {
      get {
        T val;
        lck.EnterReadLock();
        try {
          cache.TryGetValue(key, out val);
          return val;
        }
        finally { lck.ExitReadLock(); }
      }
      set {
        lck.EnterWriteLock();
        try {
          cache[key]= value;
        }
        finally { lck.ExitWriteLock(); }
      }
    }

    ///<summary>Gets an already cached value for the given <paramref name="key"/> or adds <paramref name="newVal"/> if no cached value exists.</summary>
    ///<returns>The value for the <paramref name="key"/> in the cache or <paramref name="newVal"/>.</returns>
    public T this[K key, T newVal] {
      get {
        T val;
        lck.EnterUpgradeableReadLock();
        try {
          if (cache.TryGetValue(key, out val)) return val;
          return this[key]= newVal;
        }
        finally { lck.ExitUpgradeableReadLock(); }
      }
    }

    ///<summary>Gets an already cached value for the given <paramref name="key"/> or if no cached value exists adds the value returned from <paramref name="getValue"/>.</summary>
    ///<returns>The value for the <paramref name="key"/> in the cache or the value returned from <paramref name="getValue"/>.</returns>
    public T this[K key, Func<T> getValue] {
      get {
        T val;
        lck.EnterUpgradeableReadLock();
        try {
          if (cache.TryGetValue(key, out val)) return val;
          return this[key]= getValue();
        }
        finally { lck.ExitUpgradeableReadLock(); }
      }
    }

    ///<summary>Evict entry with <paramref name="key"/>.</summary>
    ///<returns>Evicted entry or null if no value cached for <paramref name="key"/>.</returns>
    public T Evict(K key) {
      T val;
      lck.EnterUpgradeableReadLock();
      try {
        if (cache.TryGetValue(key, out val)) {
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

  }
}