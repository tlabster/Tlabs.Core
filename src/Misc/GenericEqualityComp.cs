using System;
using System.Linq;
using System.Collections.Generic;

//TODO: Move this into Tlabs.Core
namespace Tlabs.Misc {
  ///<summary>Generic impl. of a <see cref="IEqualityComparer{T}"/></summary>
  public class GenericEqualityComp<T> : IEqualityComparer<T> {
    private readonly Func<T, T, bool> isEqual;
    private readonly Func<T, int> hash;

    ///<summary>Ctor from <paramref name="isEqual"/></summary>
    public GenericEqualityComp(Func<T, T, bool> isEqual) : this(isEqual, o => 0) {  }

    ///<summary>Ctor from <paramref name="isEqual"/> and <paramref name="hash"/></summary>
    public GenericEqualityComp(Func<T, T, bool> isEqual, Func<T, int> hash) {
      if (null == (this.isEqual= isEqual)) throw new ArgumentNullException("lambdaComparer");
      if (null == (this.hash= hash)) throw new ArgumentNullException("lambdaHash");
    }
    ///<inherit/>
    public bool Equals(T x, T y) => isEqual(x, y);
    ///<inherit/>
    public int GetHashCode(T obj) => hash(obj);
  }

  ///<summary>Distinct with isEqual lambda extension.</summary>
  public static class DistinctLambdaExtension {
    ///<summary>Distinct with <paramref name="isEqual"/>.</summary>
    public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> enumerable, Func<TSource, TSource, bool> isEqual) {
      return enumerable.Distinct(new GenericEqualityComp<TSource>(isEqual));
    }
  }
}