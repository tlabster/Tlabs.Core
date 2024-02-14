using System;
using System.Buffers;
using System.IO;
using System.Threading;

namespace Tlabs.Misc {

  ///<summary>Buffer segment reader interface</summary>
  ///<remarks>Implementations of this interface are used with <see cref="SegmentSequenceBuffer"/> to expand the buffer.</remarks>
  public interface ISegmentReader : IDisposable {
    ///<summary>Read next data segment into <paramref name="buf"/></summary>
    ///<remarks>Reading less than <see cref="Memory{T}.Length"/> bytes indicates that the end of data has reached (and no more data can be expected)</remarks>
    ///<returns>Number of bytes read into <paramref name="buf"/>.</returns>
    public int Read(Memory<byte> buf);
  }

  ///<summary>Buffer segment reader from a <see cref="Stream"/></summary>
  public sealed class StreamSegmentReader : ISegmentReader {
    readonly Stream strm;

    ///<summary>Ctor from <paramref name="strm"/></summary>
    public StreamSegmentReader(Stream strm) => this.strm= strm;

    ///<inheritdoc/>
    public int Read(Memory<byte> buf) {
      int bytesRead= 0, rd;
      do {
        bytesRead+= rd= strm.Read(buf.Span.Slice(bytesRead));
      } while (rd > 0 && bytesRead < buf.Length);
      return bytesRead;
    }

    ///<inheritdoc/>
    public void Dispose() => strm.Dispose();
  }

  ///<summary>Buffer that efficiently grows or shrinks in size with minimal allocation and data copying.</summary>
  ///<remarks>
  ///<param>Access to the buffer is provided with a <see cref="ReadOnlySequence{T}"/>.</param>
  ///<param>The optional <see cref="ISegmentReader"/> is used to read more data on <see cref="Expand()"/>.
  ///As an alternative the <see cref="IBufferWriter{T}"/> interface is implemented to manually append data.</param>
  ///</remarks>
  public sealed class SegmentSequenceBuffer : IBufferWriter<byte>, IDisposable {
    readonly ISegmentReader? segReader;
    BufferSegment? start;
    BufferSegment? end;
    int endPos;  //exclusive end position
    ReadOnlySequence<byte> sequence;

    ///<summary>Ctor from option <paramref name="chunkSize"/></summary>
    public SegmentSequenceBuffer(int chunkSize= 4096) {
      if (0 >= (this.MinChunkSz= chunkSize)) throw new ArgumentException(nameof(chunkSize));
    }

    ///<summary>Ctor from <paramref name="reader"/> and option <paramref name="chunkSize"/></summary>
    public SegmentSequenceBuffer(ISegmentReader reader, int chunkSize= 4096) : this(chunkSize) {
      this.segReader= reader;
    }

    ///<summary>Ctor from <paramref name="strm"/> and option <paramref name="chunkSize"/></summary>
    public SegmentSequenceBuffer(Stream strm, int chunkSize= 4096) : this(new StreamSegmentReader(strm), chunkSize) { }

    ///<summary>Minimum buffer chunk size.</summary>
    public int MinChunkSz { get; }

    ///<summary>True if end of stream is reached.</summary>
    public bool IsEndOfStream { get; private set; }

    ///<summary>Filling level of the buffer.</summary>
    public int WrittenCount => (int)(end?.RunningIndex ?? 0) + endPos;

    ///<summary>Return buffered stream contents as as <see cref="ReadOnlySequence{T}"/>.</summary>
    public ref readonly ReadOnlySequence<byte> Sequence { get {
      if (null != start) return ref sequence;
      return ref Expand();
    }}

    ///<summary>Expand buffer with more data from stream.</summary>
    public ref readonly ReadOnlySequence<byte> Expand() {
      if (IsEndOfStream) throw new InvalidOperationException("End of data reached");
      if (null == segReader) throw new InvalidOperationException($"undefined {nameof(ISegmentReader)}");

      // fill new segment from reader
      Advance(segReader.Read(GetMemory()));
      sequence= new ReadOnlySequence<byte>(start!, 0, end!, endPos);
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

    ///<summary>Reset buffer.</summary>
    public void Reset() {
      endPos= 0;
      end?.Dispose();
      start?.Dispose();
      start= end= null;
    }

    ///<inheritdoc/>
    public void Dispose() {
      segReader?.Dispose();
      Reset();
    }

    #region IBufferWriter<byte>
    ///<inheritdoc/>
    public void Advance(int count) {
      if (   null == start
          || null == end
          || endPos + count > end.Memory.Length) throw new ArgumentOutOfRangeException("advance beyond end");
      endPos+= count;
      IsEndOfStream= endPos < end.Memory.Length;
      sequence= new ReadOnlySequence<byte>(start, 0, end, endPos);
    }

    ///<inheritdoc/>
    public Memory<byte> GetMemory(int sizeHint= 0) {
      if (IsEndOfStream) throw new InvalidOperationException("End of data reached");

      end= new BufferSegment(MinChunkSz, end);  //append new end
      start??= end;
      endPos= 0;
      return end.Buffer.Memory;
    }

    ///<inheritdoc/>
    public Span<byte> GetSpan(int sizeHint= 0) => GetMemory(sizeHint).Span;
    #endregion

    ///<summary>Advance buffer end by <paramref name="count"/> written bytes and specify <paramref name="isEndOfData"/>.</summary>
    public void Advance(int count, bool isEndOfData) {
      Advance(count);
      IsEndOfStream= isEndOfData;
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
        this.prev?.setNext(this);
      }

      public BufferSegment(BufferSegment end) {
        Buffer= end.Buffer;
        Memory= Buffer.Memory;
        RunningIndex= 0;
        end.prev?.Dispose();
      }

      void setNext(BufferSegment next) => Next= next;

      public void Dispose() {
        for (var buf= this; null != buf; buf= buf.prev) { //dispose this and all previous in the linked chain
          if (0 != Interlocked.Exchange(ref buf.isDisposed, 1)) return;
          Buffer.Dispose();
        }
      }
    }
  }
}
