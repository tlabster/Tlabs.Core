using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tlabs.Misc {

  ///<summary>Dictionary of <see cref="IEnumerable{T}"/> of key type <typeparamref name="K"/>.</summary>
  public class DictionaryList<K, T> : IEnumerable<T> {
    Dictionary<K, List<T>> dict;

    ///<summary>Default ctor.</summary>
    public DictionaryList() { dict= new Dictionary<K, List<T>>(); }

    ///<inheritdoc/>
    public IEnumerable<T> this[K key] { get => dict[key]; set => dict[key]= new List<T>(value); }

    ///<inheritdoc/>
    public ICollection<K> Keys => dict.Keys;

    ///<inheritdoc/>
    public IEnumerable<T> Values => dict.Values.SelectMany(l => l);

    ///<inheritdoc/>
    public int Count {
      get {
        int cnt= 0;
        foreach (var lst in dict.Values) cnt+= lst.Count;
        return cnt;
      }
    }

    ///<inheritdoc/>
    public bool IsReadOnly => false;

    ///<inheritdoc/>
    public void Add(K key, T value) {
      List<T> lst;
      if (!dict.TryGetValue(key, out lst)) dict.Add(key, lst= new List<T>());
      lst.Add(value);
    }

    ///<inheritdoc/>
    public void Add(KeyValuePair<K, IEnumerable<T>> pair) {
      dict.Add(pair.Key, new List<T>(pair.Value));
    }

    ///<inheritdoc/>
    public void Clear() => dict.Clear();

    ///<inheritdoc/>
    public bool Contains(T item) => Values.Any(i => i.Equals(item));

    ///<inheritdoc/>
    public bool ContainsKey(K key) => dict.ContainsKey(key);

    ///<inheritdoc/>
    public IEnumerator<T> GetEnumerator() => Values.GetEnumerator();

    ///<inheritdoc/>
    public bool Remove(K key) => dict.Remove(key);

    ///<inheritdoc/>
    public bool Remove(K key, T value) => dict[key].Remove(value);

    ///<inheritdoc/>
    public bool TryGetValue(K key, out IEnumerable<T> value) {
      List<T> lst;
      var ret= dict.TryGetValue(key, out lst);
      value= lst;
      return ret;
    }

    ///<inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}