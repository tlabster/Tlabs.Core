#pragma warning disable CA2201
using System;
using System.Runtime.CompilerServices;

namespace Tlabs.Misc {

  /// <summary>Extension methods for unmanged <c>byte[]</c> access.</summary>
  public static class ByteArrayExt {

    /// <summary>Poke <paramref name="value"/> of unmanaged type <typeparamref name="T"/> into <c>byte[]</c> at <paramref name="pos"/></summary>
    /// <remarks><paramref name="pos"/> gets advanced by the number of bytes representing type <typeparamref name="T"/>.
    /// </remarks>
    public static void Poke<T>(this byte[] mem, in T value, ref int pos) where T : unmanaged {
      mem.Peek<T>(ref pos)= value;
    }

    /// <summary>Peeks (and retuns) unmanaged type <typeparamref name="T"/> at <paramref name="pos"/></summary>
    /// <remarks>The returned value is not copied but is a reference into <paramref name="mem"/> and
    ///  <paramref name="pos"/> gets advanced by the number of bytes representing type <typeparamref name="T"/>.
    /// </remarks>
    public static ref T Peek<T>(this byte[] mem, ref int pos) where T : unmanaged {
      var sz= Unsafe.SizeOf<T>();//sizeof(T);
      if (pos + sz > mem.Length) throw new IndexOutOfRangeException();

      ref T ret= ref Unsafe.As<byte, T>(ref mem[pos]); //reference into mem array
      pos+= sz;
      return ref ret;
    }

  }

}