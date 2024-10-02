using System;
using System.Linq;
using System.IO;
using System.Text;

using Xunit;

namespace Tlabs.IO.Tests {

  public class Base64StreamTest {

    [Fact]
    public void BasicBase64ConvertTest() {
      var bin= new byte[] {0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07};
      var b64= Convert.ToBase64String(bin, Base64FormattingOptions.None);
      Assert.EndsWith("==", b64);

      Assert.Throws<FormatException>(() => Convert.FromBase64String(b64 + b64));
      Assert.Equal(bin, Convert.FromBase64String(b64));

      var bin2= bin.Concat(bin).ToArray();
      var b64_2= Convert.ToBase64String(bin2, Base64FormattingOptions.None);
      Assert.StartsWith(b64.Substring(0, b64.Length-2), b64_2);
      Assert.EndsWith("=", b64_2);
    }

    [Fact(Skip="Base64Stream.CreateForEncoding() not ready")]
    public void RoundtripBase64StreamTest() {
      var srcStrm= createBinStream(7*10 + 5);
      Assert.True(srcStrm.CanRead);

      var dstStrm= new MemoryStream((int)(2 * srcStrm.Length));
      var b64Strm= Base64Stream.CreateForEncoding(dstStrm, 10);
      Assert.True(b64Strm.CanWrite);

      var chnk= 4*10;
      b64Strm.Write(srcStrm.GetBuffer(), 0, chnk);
      srcStrm.Position= chnk;
      srcStrm.CopyTo(b64Strm);
      b64Strm.Flush();
      dstStrm.Position= 0;

      var strmRd= new StreamReader(dstStrm);
      var b64= strmRd.ReadToEnd();
      var p= b64.IndexOf('=');
      Assert.True(-1 == p || p >= b64.Length-2, "padding before end");
      var b1= srcStrm.ToArray();
      var b2= Convert.FromBase64String(b64);
      Assert.Equal(b1, b2);

#pragma warning disable SYSLIB0001    // test with Encoding.UTF7
      Encoding[] encs= new Encoding[] {null, Encoding.UTF8, Encoding.UTF7, Encoding.UTF32, Encoding.Unicode, Encoding.BigEndianUnicode, Encoding.ASCII, Encoding.Latin1};
#pragma warning restore SYSLIB0001
      dstStrm= new MemoryStream((int)(srcStrm.Length));
      foreach (var enc in encs) {
        b64Strm= Base64Stream.CreateForDecoding(createB64Stream(b64, enc), enc);
        b64Strm.CopyTo(dstStrm, null == enc ? 17 : 81920);
        Assert.Equal(b1, dstStrm.ToArray());
        dstStrm.SetLength(0);
      }
    }


    [Fact]
    public void DecodingBase64StreamTest() {
      var binStrm= createBinStream(7*10 + 5);
      var b1= binStrm.ToArray();
      var b64= Convert.ToBase64String(binStrm.ToArray());
      var dstStrm= new MemoryStream((int)(binStrm.Length));
#pragma warning disable SYSLIB0001    // test with Encoding.UTF7
      Encoding[] encs= new Encoding[] {null, Encoding.UTF8, Encoding.UTF7, Encoding.UTF32, Encoding.Unicode, Encoding.BigEndianUnicode, Encoding.ASCII, Encoding.Latin1};
#pragma warning restore SYSLIB0001
      foreach (var enc in encs) {
        var b64Strm= Base64Stream.CreateForDecoding(createB64Stream(b64, enc), enc);
        b64Strm.CopyTo(dstStrm, null == enc ? 17 : 81920);
        Assert.Equal(b1, dstStrm.ToArray());
        dstStrm.SetLength(0);
      }
    }

    MemoryStream createBinStream(int len) {
      var buf= Enumerable.Range(1, len).Select(i => (byte)(i & 255)).ToArray();
      return new MemoryStream(buf, 0, buf.Length, true, true);
    }

    MemoryStream createB64Stream(string b64, Encoding enc= null) {
      var strm= new MemoryStream(2 * b64.Length);
      var wr= new StreamWriter(strm, enc ?? Encoding.UTF8);
      wr.Write(b64);
      wr.Flush();
      strm.Position= 0;
      return strm;
    }
  }
}