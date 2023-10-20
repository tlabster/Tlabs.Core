using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Tlabs.Misc;

namespace Tlabs.Sync {

  ///<summary>Dictionary of <see cref="IEnumerable{T}"/> of key type <typeparamref name="K"/>.</summary>
  public class SyncDictionaryList<K, T> : IDictionaryList<K, T>, IReadOnlyDictList<K, T> {
    readonly Dictionary<K, LinkedList<T>> dict;

    ///<summary>Default ctor.</summary>
    public SyncDictionaryList() { dict= new Dictionary<K, LinkedList<T>>(); }

    ///<inheritdoc/>
    public IEnumerable<T> this[K key] {
      get { lock (dict) return new LinkedList<T>(dict[key]); }
      set { lock (dict) dict[key]= new LinkedList<T>(value); }
    }

    ///<inheritdoc/>
    public IEnumerable<K> Keys { get { lock (dict) return new LinkedList<K>(dict.Keys); } }

    ///<inheritdoc/>
    public IEnumerable<T> Values { get { lock (dict) return new LinkedList<T>(dict.Values.SelectMany(l => l)); } } 

    ///<inheritdoc/>
    public int Count {
      get {
        lock (dict) {
          int cnt= 0;
          foreach (var lst in dict.Values) cnt+= lst.Count;
          return cnt;
        }
      }
    }

    ///<inheritdoc/>
    public bool IsReadOnly => false;

    bool IReadOnlyDictList<K, T>.IsReadOnly => true;

    ///<inheritdoc/>
    public void Add(K key, T value) {
      lock (dict) {
        if (!dict.TryGetValue(key, out var lst)) dict.Add(key, lst= new LinkedList<T>());
        lst.AddLast(value);
      }
    }

    ///<inheritdoc/>
    public void AddRange(K key, IEnumerable<T> values) {
      lock (dict) {
        if (!dict.TryGetValue(key, out var lst)) dict.Add(key, lst= new LinkedList<T>());
        lst.AddRange(values);
      }
    }

    ///<inheritdoc/>
    public void Add(KeyValuePair<K, IEnumerable<T>> pair) {
      lock (dict) dict.Add(pair.Key, new LinkedList<T>(pair.Value));
    }

    ///<inheritdoc/>
    public void Clear() { lock (dict) dict.Clear(); }

    ///<inheritdoc/>
    public bool Contains(T item) { lock (dict) return dict.Values.Any(i => i.Equals(item)); }

    ///<inheritdoc/>
    public bool ContainsKey(K key) { lock (dict) return dict.ContainsKey(key); }

    ///<inheritdoc/>
    public IEnumerator<KeyValuePair<K, IEnumerable<T>>> GetEnumerator() {
      lock (dict) return new LinkedList<KeyValuePair<K, IEnumerable<T>>>(dict.Select(p => new KeyValuePair<K, IEnumerable<T>>(p.Key, p.Value))).GetEnumerator();
    }

    ///<inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    ///<inheritdoc/>
    public bool Remove(K key) { lock (dict) return dict.Remove(key); }

    ///<inheritdoc/>
    public bool Remove(K key, T value) { lock (dict) return dict.TryGetValue(key, out var en) && en.Remove(value); }

    ///<inheritdoc/>
    public bool TryGetValue(K key, out IEnumerable<T> value) {
      lock (dict) {
        var ret= dict.TryGetValue(key, out var lst);
        value= lst;
        return ret;
      }
    }
  }
}