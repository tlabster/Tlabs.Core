using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tlabs.Misc {
  ///<summary>Read-only dictionary of <see cref="IEnumerable{T}"/> of key type <typeparamref name="K"/>.</summary>
  public interface IReadOnlyDictList<K, T> : IEnumerable<KeyValuePair<K, IEnumerable<T>>> {
    ///<summary>The list (enumation) associated with <paramref name="key"/>.</summary>
    public IEnumerable<T> this[K key] { get; }
    ///<summary>Read-only collection of keys.</summary>
    public IEnumerable<K> Keys { get; }

    ///<summary>Read-only collection of all values (of all enumerations).</summary>
    public IEnumerable<T> Values { get; }

    ///<summary>Count of all values (of all enumerations).</summary>
    public int Count { get; }

    ///<inheritdoc/>
    public bool IsReadOnly { get; }

    ///<summary>Checks whether <paramref name="item"/> is contained in any enumerable.</summary>
    public bool Contains(T item);

    ///<summary>Checks whether <paramref name="key"/> is assocoated with an enumerable.</summary>
    public bool ContainsKey(K key);

    ///<summary>Try get <paramref name="value"/> by <paramref name="key"/>.</summary>
    public bool TryGetValue(K key, out IEnumerable<T> value);
  }

  ///<summary>Dictionary of <see cref="IEnumerable{T}"/> of key type <typeparamref name="K"/>.</summary>
  public interface IDictionaryList<K, T> : IEnumerable<KeyValuePair<K, IEnumerable<T>>> {
    ///<summary>The list (enumation) associated with <paramref name="key"/>.</summary>
    public IEnumerable<T> this[K key] { get; set; }
    ///<summary>Read-only collection of keys.</summary>
    public IEnumerable<K> Keys { get; }

    ///<summary>Read-only collection of all values (of all enumerations).</summary>
    public IEnumerable<T> Values { get; }

    ///<summary>Count of all values (of all enumerations).</summary>
    public int Count { get; }

    ///<inheritdoc/>
    public bool IsReadOnly { get; }

    ///<summary>Checks whether <paramref name="item"/> is contained in any enumerable.</summary>
    public bool Contains(T item);

    ///<summary>Checks whether <paramref name="key"/> is assocoated with an enumerable.</summary>
    public bool ContainsKey(K key);

    ///<summary>Try get <paramref name="value"/> by <paramref name="key"/>.</summary>
    public bool TryGetValue(K key, out IEnumerable<T> value);

    ///<summary>Add <paramref name="value"/> to enumerable assocoated with <paramref name="key"/>.</summary>
    public void Add(K key, T value);

    ///<summary>Add all <paramref name="values"/> to enumerable assocoated with <paramref name="key"/>.</summary>
    public void AddRange(K key, IEnumerable<T> values);

    ///<summary>Add <paramref name="pair"/>.</summary>
    public void Add(KeyValuePair<K, IEnumerable<T>> pair);

    ///<summary>Clear.</summary>
    public void Clear();

    ///<summary>Returns true if enumerable assocoated with <paramref name="key"/> was removed.</summary>
    public bool Remove(K key);

    ///<summary>Returns true if <paramref name="value"/> was rmoved from enumerable assocoated with <paramref name="key"/>.</summary>
    public bool Remove(K key, T value);
  }

  ///<summary>Dictionary of <see cref="IEnumerable{T}"/> of key type <typeparamref name="K"/>.</summary>
  public class DictionaryList<K, T> : IDictionaryList<K, T>, IReadOnlyDictList<K, T> {
    readonly Dictionary<K, List<T>> dict;
    readonly Func<K, IEnumerable<T>> defaultValue= key => throw new KeyNotFoundException(key?.ToString()??"<null>");

    ///<summary>Default ctor.</summary>
    public DictionaryList() { dict= new(); }

    ///<summary>Default ctor.</summary>
    public DictionaryList(Func<K, IEnumerable<T>> defaultValue, IEqualityComparer<K> comp= null) {
      this.defaultValue= defaultValue ?? this.defaultValue;
      this.dict= null == comp ? new() : new(comp);
    }
    ///<inheritdoc/>
    public IEnumerable<T> this[K key] {
      get {
        if (dict.TryGetValue(key, out var v)) return v;
        return this.defaultValue(key);
      }
      set => dict[key]= new List<T>(value);
    }

    ///<inheritdoc/>
    public IEnumerable<K> Keys => dict.Keys;

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

    bool IReadOnlyDictList<K, T>.IsReadOnly => true;

    ///<inheritdoc/>
    public void Add(K key, T value) {
      if (!dict.TryGetValue(key, out var lst)) dict.Add(key, lst= new List<T>());
      lst.Add(value);
    }

    ///<inheritdoc/>
    public void AddRange(K key, IEnumerable<T> values) {
      if (!dict.TryGetValue(key, out var lst)) dict.Add(key, lst= new List<T>());
      lst.AddRange(values);
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
    public IEnumerator<KeyValuePair<K, IEnumerable<T>>> GetEnumerator() => dict.Select(p => new KeyValuePair<K, IEnumerable<T>>(p.Key, p.Value)).GetEnumerator();

    ///<inheritdoc/>
    public bool Remove(K key) => dict.Remove(key);

    ///<inheritdoc/>
    public bool Remove(K key, T value) => dict.TryGetValue(key, out var en) && en.Remove(value);

    ///<inheritdoc/>
    public bool TryGetValue(K key, out IEnumerable<T> value) {
      var ret= dict.TryGetValue(key, out var lst);
      value= lst;
      return ret;
    }

    ///<inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  }
}