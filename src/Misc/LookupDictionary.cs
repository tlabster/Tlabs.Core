using System;
using System.Collections;
using System.Collections.Generic;

namespace Tlabs.Misc {

  ///<summary>Look-up dictionary.</summary>
  ///<remarks>Reading by looking-up an entry does not mutate the dictionary.</remarks>
  public class LookupDictionary<K, T> : IReadOnlyDictionary<K, T> {
    ///<summary>look-up dictionary.</summary>
    protected IDictionary<K, T> table; 
    readonly Func<K, T> defaultValue;

    ///<summary>Default ctor.</summary>
    public LookupDictionary() { this.table= new Dictionary<K, T>(); }
    ///<summary>Ctor from <paramref name="comp"/>.</summary>
    public LookupDictionary(IEqualityComparer<K> comp) { this.table= new Dictionary<K, T>(comp); }

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
      this.table= new Dictionary<K, T>(entries);
      this.defaultValue= defaultValue;
    }
    ///<summary>Ctor from <paramref name="entries"/> and <paramref name="defaultValue"/> delegate and <paramref name="comp"/>.</summary>
    public LookupDictionary(IEnumerable<KeyValuePair<K, T>> entries, Func<K, T> defaultValue, IEqualityComparer<K> comp) {
      this.table= new Dictionary<K, T>(entries, comp);
      this.defaultValue= defaultValue;
    }

    ///<summary>Ctor from <paramref name="dict"/> and <paramref name="defaultValue"/> delegate.</summary>
    public LookupDictionary(IDictionary<K, T> dict, Func<K, T> defaultValue) {
      this.table= dict ?? new Dictionary<K, T>();
      this.defaultValue= defaultValue;
    }

    ///<inheritdoc/>
    public T this[K key] {
      get {
        if (table.TryGetValue(key, out var val)) return val;
        if (null == defaultValue) throw new KeyNotFoundException(key.ToString());
        return defaultValue(key);
      }
      set => table[key]= value;
    }

    ///<inheritdoc/>
    public IEnumerable<K> Keys => table.Keys;

    ///<inheritdoc/>
    public IEnumerable<T> Values => table.Values;

    ///<inheritdoc/>
    public int Count => table.Count;

    ///<inheritdoc/>
    public bool ContainsKey(K key) => table.ContainsKey(key);

    ///<inheritdoc/>
    public IEnumerator<KeyValuePair<K, T>> GetEnumerator() => table.GetEnumerator();

    ///<inheritdoc/>
    public bool TryGetValue(K key, out T value) => table.TryGetValue(key, out value);

    ///<inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => table.GetEnumerator();
  }
}