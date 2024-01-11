

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Tlabs.Sync;

#nullable enable

namespace Tlabs.Misc {

  ///<summary>Buffered <see cref="TextReader"/>.</summary>
  ///<remarks>
  ///</remarks>
  public class BufferedReader : TextReader {
    static readonly ILogger log= App.Logger<BufferedReader>();

    readonly ConcurrentQueue<string?> lineQ= new();
    readonly SyncMonitor<bool> pendingRead= new();
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification= "StringReader does not need to be disposed")]
    StringReader? currLine;
    TaskCompletionSource<string?>? readTsk;
    bool endOfText;

    ///<summary>Ctor from <paramref name="ctk"/></summary>
    public BufferedReader(CancellationToken ctk= default) {
      ctk.Register(() => {
        log.LogDebug("Cancel " + nameof(BufferedReader));
        this.endOfText= true;
        if (pendingRead.Value) pendingRead.SignalPermanent(false);  //release any blocked reader
        readTsk?.TrySetResult(null);
        readTsk= null;
      });
    }

    bool tryDequeueLine(out string? line) {
      if (!this.lineQ.TryDequeue(out line)) return false;
      this.endOfText= endOfText || null == line;
      return true;
    }

    ///<summary>Event fired on closing</summary>
    public event Action? Closing;

    ///<summary>Buffer one <paramref name="line"/></summary>
    ///<remarks>if <paramref name="line"/> is null the reader is closed.</remarks>
    public BufferedReader BufferLine(string? line) {
      if (endOfText && null != line) throw new InvalidOperationException("End of stream already reached.");
      lineQ.Enqueue(line);

      if (pendingRead.Value) return loadLine();
      if (   readTsk is {} rdt
          && tryDequeueLine(out line)) {
        readTsk= null;
        log.LogTrace("Return async line");
        rdt?.TrySetResult(line);
      }
      return this;
    }

    BufferedReader loadLine() {
      if (tryDequeueLine(out var line)) {
        this.currLine= new StringReader(null == line ? "" : line + "\n");
        log.LogTrace("Char buffer loaded...");
        pendingRead.SignalPermanent(false);
      }
      return this;
    }

    ///<inheritdoc/>
    public override int Peek() {
      int ch= -1;
      while (   !this.endOfText
             && (null == currLine || -1 == (ch= currLine.Peek())))
      {
        pendingRead.Value= true;
        loadLine();
        if (pendingRead.Value) {
          log.LogTrace("Waiting for next line...");
          pendingRead.WaitForSignal();      //wait for next loadLine()
          pendingRead.ResetSignal();
        }
      }
      if (-1 == ch) currLine= null;
      return ch;
    }

    ///<inheritdoc/>
    public override int Read() {
      if (-1 == this.Peek()) return -1;
      return currLine?.Read() ?? -1;
    }

    ///<inheritdoc/>
    public override string? ReadLine() {
      if (this.endOfText) return null;
      if (   tryReadEndOfLine(out var line)
          || tryDequeueLine(out line)) return line;
      line= base.ReadLine();
      this.currLine= null;
      return line;
    }

    bool tryReadEndOfLine(out string? line) {
      if (currLine is not {} crLn) { line= null; return false; }
      currLine= null;
      line= crLn?.ReadLine();
      this.endOfText= this.endOfText || null == line;
      return true;
    }

    ///<inheritdoc/>
    public override Task<string?> ReadLineAsync() {
      if (this.endOfText) return Task.FromResult((string?)null);
      if (   tryReadEndOfLine(out var line)
          || tryDequeueLine(out line)) return Task.FromResult(line);

      if (null != this.readTsk) throw new InvalidOperationException("ReadLineAsync() still pending.");
      this.readTsk= new TaskCompletionSource<string?>();
      return this.readTsk.Task;
    }

    ///<inheritdoc/>
    protected override void Dispose(bool disposing) {
      if (disposing) {
        log.LogTrace("Disposing BufferdReader");
        BufferLine(null);
        Closing?.Invoke();
      }
      base.Dispose(disposing);
    }
  }
}
