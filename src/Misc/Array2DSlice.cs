using System;
using System.Collections;
using System.Collections.Generic;

namespace Tlabs.Misc {
  ///<summary>Row slice of an 2D array of <typeparamref name="T"/>.</summary>
  public class Array2DRowSlice<T> : IReadOnlyList<T> {
    readonly T[,] data;
    readonly int row;

    ///<summary>Ctor from <paramref name="data"/> and <paramref name="row"/>.</summary>
    public Array2DRowSlice(T[,] data, int row) {
      if (null == (this.data= data)) throw new ArgumentNullException(nameof(data));
      // Defer the bounds check to the indexer if (row < 0 || row >= data.GetLength(0)) throw new IndexOutOfRangeException();
      this.row= row;
    }

    ///<summary>Index into row.</summary>
    public T this[int index] => data[row, index];

    ///<summary>data.GetLength(1)</summary>
    public int Count => data.GetLength(1);

    ///<summary>Row values enumeration</summary>
    public IEnumerator<T> GetEnumerator() {
      for (int l= 0, n= Count; l < n; ++l)
        yield return this[l];
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
  }
}