using System;
using System.IO;

using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Misc.Tests {
#pragma warning disable CS0618

  public class ReadStreamBufferTest {
    static byte[] buf256;
    private readonly ITestOutputHelper tstout;


    static ReadStreamBufferTest() {
      buf256= new byte[256];
      for (int l= 0; l < buf256.Length; ++l) buf256[l]= (byte)l;
    }

    public ReadStreamBufferTest(ITestOutputHelper output) => this.tstout= output;

    [Fact]
    public void OneSegmentTest() {
      var strm= new MemoryStream(buf256);
      using var rdBuf= new ReadStreamBuffer(strm);
      var seq= rdBuf.Sequence;
      Assert.False(seq.IsEmpty);
      Assert.True(seq.IsSingleSegment);
      Assert.Equal(buf256.Length, seq.Length);
      Assert.Equal(0, rdBuf.Shrink());
      Assert.Throws<InvalidOperationException>(() => rdBuf.Expand());
    }

    [Fact]
    public void OneSmallSegmentTest() {
      const int sz= 5;
      var strm= new MemoryStream(buf256);
      using var rdBuf= new ReadStreamBuffer(strm, sz);
      var seq= rdBuf.Sequence;
      Assert.True(seq.IsSingleSegment);
      Assert.True (seq.Length >= sz);
      Assert.Equal(0, rdBuf.Shrink());
    }

    [Fact]
    public void ExpandSegmentTest() {
      int sz= 5;
      var strm= new MemoryStream(buf256);
      using var rdBuf= new ReadStreamBuffer(strm, sz);
      var seq= rdBuf.Sequence;
      Assert.False(seq.IsEmpty);
      Assert.True(seq.IsSingleSegment);
      Assert.True(seq.Length >= sz);
      sz= (int)seq.Length;
      var segCnt= 1;
      while (seq.Length < buf256.Length) {
        seq= rdBuf.Expand();
        ++segCnt;
        Assert.False(seq.IsSingleSegment);
      }
      Assert.Equal(buf256.Length, seq.Length);
      Assert.True(segCnt >= buf256.Length/sz);
      var memPos= 0;
      foreach (var mem in seq) {
        --segCnt;
        var span= mem.Span;
        for(var l= 0; l < span.Length; ++l) Assert.Equal(buf256[memPos+l], span[l]);
        memPos+= span.Length;
      }
      Assert.Equal(0, segCnt);
    }
  }
}
#pragma warning restore
