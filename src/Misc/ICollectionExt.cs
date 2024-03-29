﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Tlabs.Misc {

  /// <summary>Extension methods for generic collection interfaces.</summary>
  public static class ICollectionExt {

    ///<summary>Adds the elements of the specified <paramref name="rng"/> to the end of collection <paramref name="col"/>.</summary>
    public static void AddRange<T>(this ICollection<T> col, IEnumerable<T> rng) {
      ArgumentNullException.ThrowIfNull(col);
      if (null == rng) return;
      if (col is List<T> specificLst) {
        specificLst.AddRange(rng);
        return;
      }

      foreach (var itm in rng)
        col.Add(itm);
    }

#if false
    ///<summary>Adds the elements of the specified collection <paramref name="coll"/> to the end of <see cref="IList{T}"/>.</summary>
    public static void AddRange<T>(this IList<T> lst, IEnumerable<T> coll) {
      if (null == lst) throw new ArgumentNullException(nameof(lst));
      if (null == coll) return;
      if (lst is List<T> specificLst) {
        specificLst.AddRange(coll);
        return;
      }

      foreach (var itm in coll)
        lst.Add(itm);
    }

    ///<summary>Adds the elements of the specified collection <paramref name="coll"/> to <see cref="ISet{T}"/>.</summary>
    public static void AddRange<T>(this ISet<T> set, IEnumerable<T> coll) {
      if (null == set) throw new ArgumentNullException(nameof(set));
      if (null == coll) return;
      foreach (var itm in coll)
        set.Add(itm);
    }
#endif

    ///<summary>Gets an exisiting or adds the <paramref name="itm"/> to the <see cref="ISet{T}"/>.</summary>
    public static T GetOrAdd<T>(this ISet<T> set, T itm, IEqualityComparer<T>? cmp= null) {
      ArgumentNullException.ThrowIfNull(set);
      ArgumentNullException.ThrowIfNull(itm);

      if (set is HashSet<T> hashSet) {
        if (hashSet.TryGetValue(itm, out var itm2)) return itm2;
        hashSet.Add(itm);
        return itm;
      }

      if (set.Add(itm)) return itm;
      return   (null != cmp)
             ? set.Where(i => cmp.Equals(i, itm)).First()
             : set.Where(i => Object.Equals(i, itm)).First();
    }

    ///<summary>Compares <see cref="IDictionary{K,T}"/> contents for equality.</summary>
    public static bool ContentEquals<K, T>(this IDictionary<K, T> dict, IDictionary<K, T> other) {
      ArgumentNullException.ThrowIfNull(dict);
      ArgumentNullException.ThrowIfNull(other);

      if (dict.Count != other.Count) return false;
      return dict.All(p => other.TryGetValue(p.Key, out var val) && object.Equals(p.Value, val));
    }

    ///<summary>Compares <see cref="IDictionary{K,T}"/> contents for equality.</summary>
    public static bool ContentEquals<K, T>(this IDictionary<K, T> dict, IDictionary<K, T> other, IEqualityComparer<T> cmp) {
      ArgumentNullException.ThrowIfNull(dict);
      ArgumentNullException.ThrowIfNull(other);
      ArgumentNullException.ThrowIfNull(cmp);

      if (dict.Count != other.Count) return false;
      return dict.All(p => other.TryGetValue(p.Key, out var val) && cmp.Equals(p.Value, val));
    }

    ///<summary>Compares <see cref="IEnumerable{T}"/> contents for equality.</summary>
    public static bool ContentEquals<T>(this IEnumerable<T> seq, IEnumerable<T> other) where T : class {
      ArgumentNullException.ThrowIfNull(seq);
      ArgumentNullException.ThrowIfNull(other);

      if (seq.TryGetNonEnumeratedCount(out var seqCnt) && other.TryGetNonEnumeratedCount(out var otherCnt) && seqCnt != otherCnt) return false;
      var set= other.ToHashSet();
      return seq.All(itm => set.Contains(itm));
    }

    ///<summary>Compares <see cref="IEnumerable{T}"/> contents for equality.</summary>
    public static bool ContentEquals<T>(this IEnumerable<T> seq, IEnumerable<T> other, IEqualityComparer<T> cmp) where T : class {
      ArgumentNullException.ThrowIfNull(seq);
      ArgumentNullException.ThrowIfNull(other);
      ArgumentNullException.ThrowIfNull(cmp);

      if (seq.TryGetNonEnumeratedCount(out var seqCnt) && other.TryGetNonEnumeratedCount(out var otherCnt) && seqCnt != otherCnt) return false;
      var set= other.ToHashSet(cmp);
      return seq.All(itm => set.Contains(itm, cmp));
    }
  }

  /// <summary>Interface of an object that implements <see cref="ToString(T)"/>.</summary>
  ///<remarks>To be used in combination with <see cref="IEqualityComparer{T}"/></remarks>
  public interface IStringConvertible<T> {
    /// <summary>Returns a string representation of <paramref name="o"/>.</summary>
    string ToString(T o);
  }
}