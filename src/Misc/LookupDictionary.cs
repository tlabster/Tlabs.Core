using System;
using System.Collections;
using System.Collections.Generic;

namespace Tlabs.Misc {

  ///<summary>Look-up dictionary.</summary>
  ///<remarks>Reading by looking-up an entry does not mutate the dictionary.</remarks>
  public class LookupDictionary<K, T> : IReadOnlyDictionary<K, T> {
    ///<summary>look-up dictionary.</summary>
    protected IDictionary<K, T> dict; 
    readonly Func<K, T> defaultValue;

    ///<summary>Default ctor.</summary>
    public LookupDictionary() { this.dict= new Dictionary<K, T>(); }
    ///<summary>Ctor from <paramref name="comp"/>.</summary>
    public LookupDictionary(IEqualityComparer<K> comp) { this.dict= new Dictionary<K, T>(comp); }

    ///<summary>Ctor from <paramref name="defaultValue"/> delegate.</summary>
    public LookupDictionary(Func<K, T> defaultValue) : this() {
      this.defaultValue= defaultValue;
    }
    ///<summary>Ctor from <paramref name="defaultValue"/> delegate and <paramref name="comp"/>.</summary>
    public LookupDictionary(Func<K, T> defaultValue, IEqualityComparer<K> comp) : this(comp) {
      this.defaultValue= defaultValue;
    }
    ///<summary>Ctor from <paramref name="entries"/> and <paramref name="defaultValue"/> delegate.</summary>
    public LookupDictionary(IEnumerable<KeyValuePair<K, T>> entries, Func<K, T> defaultValue) {
      this.dict= new Dictionary<K, T>(entries);
      this.defaultValue= defaultValue;
    }
    ///<summary>Ctor from <paramref name="entries"/> and <paramref name="defaultValue"/> delegate and <paramref name="comp"/>.</summary>
    public LookupDictionary(IEnumerable<KeyValuePair<K, T>> entries, Func<K, T> defaultValue, IEqualityComparer<K> comp) {
      this.dict= new Dictionary<K, T>(entries, comp);
      this.defaultValue= defaultValue;
    }

    ///<summary>Ctor from <paramref name="dict"/> and <paramref name="defaultValue"/> delegate.</summary>
    public LookupDictionary(IDictionary<K, T> dict, Func<K, T> defaultValue) {
      this.dict= dict ?? new Dictionary<K, T>();
      this.defaultValue= defaultValue;
    }

    ///<inheritdoc/>
    public T this[K key] {
      get {
        if (dict.TryGetValue(key, out var val)) return val;
        if (null == defaultValue) throw new KeyNotFoundException(key.ToString());
        return defaultValue(key);
      }
      set => dict[key]= value;
    }

    ///<summary>Adds a new value for <paramref name="key"/> by using the <c>defaultValue</c> delegate specified with the ctor if the key does not already exist.</summary>
    ///<returns>The new value, or the exisitng value if the <paramref name="key"/> exists.</returns>
    public T GetOrAdd(K key) {
      if (dict.TryGetValue(key, out var val)) return val;
      return dict[key]= defaultValue(key);
    }

    ///<inheritdoc/>
    public IEnumerable<K> Keys => dict.Keys;

    ///<inheritdoc/>
    public IEnumerable<T> Values => dict.Values;

    ///<inheritdoc/>
    public int Count => dict.Count;

    ///<inheritdoc/>
    public bool ContainsKey(K key) => dict.ContainsKey(key);

    ///<inheritdoc/>
    public IEnumerator<KeyValuePair<K, T>> GetEnumerator() => dict.GetEnumerator();

    ///<inheritdoc/>
    public bool TryGetValue(K key, out T value) => dict.TryGetValue(key, out value);

    ///<inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();
  }
}