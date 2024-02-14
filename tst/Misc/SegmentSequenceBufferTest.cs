using System;
using System.IO;

using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Misc.Tests {

  public class SegmentSequenceBufferTest {
    static byte[] buf256;
    private readonly ITestOutputHelper tstout;


    static SegmentSequenceBufferTest() {
      buf256= new byte[256];
      for (int l= 0; l < buf256.Length; ++l) buf256[l]= (byte)l;
    }

    public SegmentSequenceBufferTest(ITestOutputHelper output) => this.tstout= output;

    [Fact]
    public void OneSegmentTest() {
      var strm= new MemoryStream(buf256);
      using var rdBuf= new SegmentSequenceBuffer(strm);
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
      using var rdBuf= new SegmentSequenceBuffer(new StreamSegmentReader(strm), sz);
      var seq= rdBuf.Sequence;
      Assert.True(seq.IsSingleSegment);
      Assert.True (seq.Length >= sz);
      Assert.Equal(0, rdBuf.Shrink());
    }

    [Fact]
    public void ExpandSegmentTest() {
      int sz= 5;
      var strm= new MemoryStream(buf256);
      using var rdBuf= new SegmentSequenceBuffer(new StreamSegmentReader(strm), sz);
      var seq= rdBuf.Sequence;
      Assert.False(seq.IsEmpty);
      Assert.True(seq.IsSingleSegment);
      Assert.True(seq.Length >= sz);
      sz= (int)seq.Length;    //real segment length
      var segCnt= 1;
      while (seq.Length < buf256.Length) {    //exxpand to entire stream
        seq= rdBuf.Expand();
        ++segCnt;
        Assert.False(seq.IsSingleSegment);
      }
      Assert.False(seq.IsSingleSegment);
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

      var offs= 251;
      var pos= seq.GetPosition(offs);
      Assert.Equal(offs, seq.GetOffset(pos));
      Assert.Equal(offs, buf256[offs]);
      var got= seq.TryGet(ref pos, out var memSeg);
      Assert.True(got);
      Assert.Equal(buf256[offs], memSeg.Span[0]);

      var of2= offs - (int)rdBuf.Shrink();
      seq= rdBuf.Sequence;
      Assert.True(seq.IsSingleSegment);
      pos= seq.GetPosition(of2);
      Assert.Equal(of2, seq.GetOffset(pos));
      Assert.True(seq.TryGet(ref pos, out memSeg));
      Assert.Equal(buf256[offs], memSeg.Span[0]);
      Assert.Equal(buf256.Length-offs, memSeg.Length);

    }
  }
}