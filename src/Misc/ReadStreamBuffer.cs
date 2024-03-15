using System;
using System.Buffers;
using System.IO;
using System.Threading;

#nullable enable

namespace Tlabs.Misc {

  ///<summary><see cref="Stream"/> reader that buffers data as <see cref="ReadOnlySequence{T}"/></summary>
  ///<remarks>
  ///This provides a chunk of data read from the stream as <see cref="Sequence"/>.
  ///if not <see cref="IsEndOfStream"/> <see cref="Expand()"/> reads another chunk of data from the stream and appends it to <see cref="Sequence"/>.
  ///This is usefull as a buffer that can efficiently grow with no internal data copying on resize...
  ///</remarks>
  public sealed class ReadStreamBuffer : IDisposable {
    readonly Stream strm;
    bool isEndOfStream;
    BufferSegment? start;
    BufferSegment? end;
    int endPos;  //exclusive end position
    ReadOnlySequence<byte> sequence;

    ///<summary>Ctor from <paramref name="stream"/> and option <paramref name="chunkSize"/></summary>
    [Obsolete("Use SegmentReadBuffer as drop-in replacement", false)]
    public ReadStreamBuffer(Stream stream, int chunkSize= 4096) {
      if (null == (this.strm= stream)) throw new ArgumentNullException(nameof(stream));
      if (0 >= (this.MinChunkSz= chunkSize)) throw new ArgumentException(nameof(chunkSize));
    }

    ///<summary>Ctor from previos <paramref name="buffer"/></summary>
    public ReadStreamBuffer(ReadStreamBuffer buffer) {
      this.strm= buffer.strm;
      this.MinChunkSz= buffer.MinChunkSz;
      buffer.Dispose();
    }
    ///<summary>Minimum buffer chunk size.</summary>
    public int MinChunkSz { get; }

    ///<summary>True if end of stream is reached.</summary>
    public bool IsEndOfStream => isEndOfStream;

    ///<summary>Return buffered stream contents as as <see cref="ReadOnlySequence{T}"/>.</summary>
    public ref readonly ReadOnlySequence<byte> Sequence { get {
      if (null != start) return ref sequence;
      return ref Expand();
    }}

    ///<summary>Expand buffer with more data from stream.</summary>
    public ref readonly ReadOnlySequence<byte> Expand() {   //***TODO: Make this more generic to expand even from different data sources
      if (isEndOfStream) throw new InvalidOperationException("End of stream");
      /* Append segment:
       */
      var newSegment= new BufferSegment(MinChunkSz, end);
      end?.SetNext(newSegment);
      end= newSegment;

      if (null == start) start= end;

      // append data from stream
      endPos= 0;
      int bytesRead;
      do {
        bytesRead= strm.Read(end.Buffer.Memory.Span.Slice(endPos));
        endPos+= bytesRead;
      } while (bytesRead > 0 && endPos < end.Buffer.Memory.Length);
      isEndOfStream= endPos < newSegment.Buffer.Memory.Length;
      sequence= new ReadOnlySequence<byte>(start, 0, end, endPos);
      return ref sequence;
    }

    ///<summary>Shrink buffer to last segment and return count of bytes shrinked.</summary>
    public long Shrink() {
      if (null != end) {
        var shrinkCnt= end.RunningIndex;
        start= end= new BufferSegment(end);
        sequence= new ReadOnlySequence<byte>(start, 0, end, endPos);
        return shrinkCnt;
      }
      return 0;
    }

    ///<inheritdoc/>
    public void Dispose() {
      strm.Dispose();
      start?.Dispose();
      end?.Dispose();
      start= end= null;
    }



    sealed class BufferSegment : ReadOnlySequenceSegment<byte>, IDisposable {
      public IMemoryOwner<byte> Buffer { get; }
      readonly BufferSegment? prev;
      int isDisposed;

      public BufferSegment(int size, BufferSegment? prev) {
        Buffer= MemoryPool<byte>.Shared.Rent(size);
        this.prev= prev;

        Memory= Buffer.Memory;
        RunningIndex= prev?.RunningIndex + prev?.Memory.Length ?? 0;
        // this.prev?.SetNext(this);
      }

      public BufferSegment(BufferSegment end) {
        Buffer= end.Buffer;
        Memory= Buffer.Memory;
        RunningIndex= 0;
        end.prev?.Dispose();
      }

      public void SetNext(BufferSegment next) => Next= next;

      public void Dispose() {
        if (0 != Interlocked.Exchange(ref isDisposed, 1)) return;
        Buffer.Dispose();
        prev?.Dispose();
      }
    }
  }
}
#nullable restore
