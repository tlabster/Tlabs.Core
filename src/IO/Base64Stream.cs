using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace Tlabs.IO {

  /// <summary>Stream implementation to Base64 decode or encode a <sse cref="Stream"/></summary>
  public abstract class Base64Stream : Stream {
    /// <summary>Creates a <sse cref="Base64Stream"/> to Base64 encode binary data being written to <paramref name="strm"/></summary>
    /// <remarks>
    /// The binary data being written to the returned stream will be encoded as a Base46 text (with UTF-8 character encoding) and written to the passed <paramref name="strm"/>.
    /// </remarks>
    internal static Base64Stream CreateForEncoding(Stream strm, int bufSz= 1024) => new ChunkedEncoder(strm, bufSz);    //***TODO: Finish this
    /// <summary>Creates a <sse cref="Base64Stream"/> to decode Base64 encoded data being read from <paramref name="strm"/></summary>
    /// <remarks>
    /// The returned stream allows to read binary data being decoded from the Base64 encoded data provided by the passed <paramref name="strm"/>.
    /// (With <paramref name="enc"/> the optional character encoding of the input <paramref name="strm"/> can be specified (defaults to UTF-8))
    /// </remarks>
    public static Base64Stream CreateForDecoding(Stream strm, Encoding? enc= null) => new Decoder(strm, enc);

    /// <summary>Internal <sse cref="Stream"/></summary>
    protected Stream strm;
    /// <summary>Internal ctor from <sse cref="Stream"/></summary>
    protected Base64Stream(Stream strm) {
      this.strm= strm;
    }
    /// <inheritdoc/>
    public override bool CanRead => strm.CanRead;
    /// <inheritdoc/>
    public override bool CanWrite => strm.CanWrite;
    /// <inheritdoc/>
    public override bool CanSeek => false;
    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => strm.Read(buffer, offset, count);
    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => strm.Write(buffer, offset, count);
    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();
    /// <inheritdoc/>
    public override long Position {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
    }
    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();
    /// <inheritdoc/>
    public override void Flush() => strm.Flush();
    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <inheritdoc/>
    protected override void Dispose(bool disposing) {
      if (disposing) strm.Dispose();
      base.Dispose(disposing);
    }

    //*** TODO: Using a BufferedStream does not work! (See unit test.) Need to develop a working chunk buffer!!!
    //Apply a BufferedStream in order to encode the data streams in chunks with a size of a multiple of 3.
    //This is important to avoid having paddings "=" occure in the middle of the base64 text.
    class ChunkedEncoder : Base64Stream {
      int chnkSz;
      public ChunkedEncoder(Stream strm, int bufSz) : base(new BufferedStream(new Encoder(strm), 3 * bufSz)) { this.chnkSz= 3 * bufSz; }  // must be multiple of 3

      public override bool CanRead => false;
      public override void Write(byte[] buffer, int offset, int count) {
        if (count <= this.chnkSz) {
          strm.Write(buffer, offset, count);
          return;
        }
        /* Buffer sizes > chnkSz are not buffered by BufferedStream and thus need to be split up into a
         * multiple of 3 sized part and (buffered) remainder:
         */
        var r= count % 3;
        strm.Write(buffer, offset, count-r);
        if (r > 0) strm.Write(buffer, offset+count-r, r); //buffer remainder to avoid intermediate padding
      }
    }

    class Encoder : Base64Stream {
      private StreamWriter strmWriter;
      private bool mustBeLast;
      public Encoder(Stream strm) : base(strm) {
        this.strmWriter= new StreamWriter(strm);
      }

      public override bool CanRead => false;
      public override void Write(byte[] buffer, int offset, int count) {
        if (mustBeLast) throw new InvalidOperationException("Final padded chunk already written");
        mustBeLast= 0 != count % 3;
        var b64= Convert.ToBase64String(buffer, offset, count);
        strmWriter.Write(b64);
      }
      public override void Flush() {
        strmWriter.Flush();
      }
      protected override void Dispose(bool disposing) {
        if (disposing) strmWriter.Dispose();
        base.Dispose(disposing);
      }
    }

    class Decoder : Base64Stream {
      private StreamReader strmReader;
      private byte[]? readBuf;
      private int readPos= 0;

      public Decoder(Stream strm, Encoding? enc) : base(strm) {
        this.strmReader= new StreamReader(strm, enc ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      }

      public override int Read(byte[] buffer, int offset, int count) {
        if (null == strmReader) throw new NotSupportedException();
        var rd= 0;

        if (null == readBuf) {
          var chBuf= ArrayPool<char>.Shared.Rent(4 * 512);  //try read multiples of 4
          try {
            rd= strmReader.Read(chBuf, 0, chBuf.Length);
            readBuf= Convert.FromBase64CharArray(chBuf, 0, rd);
            readPos= 0;
          }
          finally { ArrayPool<char>.Shared.Return(chBuf); }
        }
        rd= Math.Min(count, readBuf.Length - readPos);
        Array.Copy(readBuf, readPos, buffer, offset, rd);
        readPos+= rd;

        if (readPos >= readBuf.Length) {
          readBuf= null;
          readPos= 0;
        }
        return rd;
      }

      protected override void Dispose(bool disposing) {
        if (disposing) strmReader.Dispose();
        base.Dispose(disposing);
      }
    }
  }

}