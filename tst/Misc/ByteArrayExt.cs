using System.Runtime.CompilerServices;
using Xunit;

namespace Tlabs.Misc.Test {

  public class ByteArrayExt {

    [Fact]
    public void StructTest() {
      var sct= new TstStruct {
        one= byte.MaxValue,
        two= 2,
        three= true,
        three2= true,
        four= -4,
        five= -5.0F
      };
      var sz= Unsafe.SizeOf<TstStruct>();

      var mem= new byte[512];
      int p= 0;
      mem.Poke(sct, ref p);
      Assert.NotEqual(0, p);    //p must have been incremented

      p= 0;
      ref var sctRef= ref mem.Peek<TstStruct>(ref p);
      Assert.Equal(sct, sctRef);

      sctRef.one= 11;
      Assert.Equal(sctRef.one, (int)mem[0]);    //sctRef points into mem[]!!!
      Assert.NotEqual(sct, sctRef);
      sctRef.one= sct.one;
      Assert.Equal(sct, sctRef);

      var sctCpy= sctRef;   //make copy
      sctCpy.two= 11;
      Assert.Equal(sct.two, (int)mem[Unsafe.SizeOf<int>()]);    //sctCpy DOES NOT point into mem[]!!!

      p= 8; //missalign
      sctRef= mem.Peek<TstStruct>(ref p);
      Assert.NotEqual(sct, sctRef);
    }

    struct TstStruct {
      public byte one;
      public int two;
      public bool three;
      public bool three2;
      public float five;
      public long four;
    }

    [Fact]
    public void ArrayTest() {
      var mem= new byte[512];
      int p= 0;
      var intAry= new int[] { -1, -2, -3 };

      for (var l= 0; l < intAry.Length; ++l) {
        mem.Poke(intAry[l], ref p);
      }
      Assert.NotNull(p);

      p= 0;
      for (var l = 0; l < intAry.Length; ++l) {
        Assert.Equal(intAry[l], mem.Peek<int>(ref p));
      }
    }
  }
}