using System;
using System.Collections.Generic;
using System.Linq;

namespace Tlabs.Misc {

  /// <summary>Enumerable utils.</summary>
  public class EnumerableUtil {

    /// <summary>Return <see cref="IEnumerable{T}"/> with one <paramref name="entry"/>.</summary>
    public static IEnumerable<T> One<T>(T entry) {
      yield return entry;
    }

    /// <summary>Return <see cref="IEnumerable{T}"/> with all parameter <paramref name="entries"/>.</summary>
    public static IEnumerable<T> Params<T>(params T[] entries) => entries;
  }
}