using System;
using System.Collections;
using System.Collections.Generic;

namespace Tlabs.Misc {

  ///<summary>Object look-up table.</summary>
  public class LookupTable<K, T> : IReadOnlyDictionary<K, T> {
    ///<summary>Look-up table.</summary>
    protected Dictionary<K, T> table; 
    private Func<K, T> create;

    ///<summary>Default ctor.</summary>
    public LookupTable() { this.table= new Dictionary<K, T>(); }
    ///<summary>Ctor from <paramref name="create"/> delegate.</summary>
    public LookupTable(Func<K, T> create) : this() {
      this.create= create;
    }
    ///<summary>Ctor from <paramref name="entries"/> and <paramref name="create"/> delegate.</summary>
    public LookupTable(IEnumerable<KeyValuePair<K, T>> entries, Func<K, T> create) {
      this.table= new Dictionary<K, T>(entries);
      this.create= create;
    }

    ///<inheritdoc/>
    public T this[K key] {
      get {
        T val;
        if (table.TryGetValue(key, out val)) return val;
        if (null == create) throw new KeyNotFoundException(key.ToString());
        return table[key]= create(key);
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