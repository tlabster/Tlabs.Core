using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tlabs.Sync {

  ///<summary>Buffered action runner</summary>
  public sealed class BufferedActionRunner : IDisposable {
    ActionBuffer actBuf;

    ///<summary>Run <paramref name="action"/> after <paramref name="bufferTime"/> has been elapsed.</summary>
    ///<remarks>Repeated invocation before <paramref name="bufferTime"/> eleapsed are resetting the the timer...</remarks>
    public void Run(int bufferTime, Action action) {
      if (0 == bufferTime) {
        action(); //run action immediately
        return;
      }

      var newBuf= new ActionBuffer(bufferTime);
      while (true) {
        var oldBuf= Interlocked.CompareExchange<ActionBuffer>(ref this.actBuf, newBuf, null);
        if (null == oldBuf) {
          newBuf.Schedule(action);
          return;
        }
        if (oldBuf == Interlocked.CompareExchange<ActionBuffer>(ref this.actBuf, null, oldBuf)) {
          oldBuf.Dispose();
          newBuf.BufTime= oldBuf.BufTime ?? newBuf.BufTime;
        }
      }
    }

    ///<inheritdoc/>
    public void Dispose() {
      actBuf?.Dispose();
      this.actBuf= null;
      GC.SuppressFinalize(this);
    }

    private sealed class ActionBuffer : IDisposable {
      int delay;
      CancellationTokenSource cts;
      public ActionBuffer(int bufferTime) {
        this.cts= new();
        this.delay= bufferTime;
      }

      public DateTime? BufTime= DateTime.Now;

      public void Schedule(Action action) {
        if ((DateTime.Now - BufTime)?.TotalMilliseconds >= delay) {
          runAction(action);
          return;
        }
        Task.Delay(this.delay, this.cts.Token)
            .ContinueWith(t => runAction(action), TaskContinuationOptions.NotOnCanceled);
      }

      private void runAction(Action action) {
        BufTime= null;
        Dispose();
        action();
      }

      public void Dispose() {
        var ctSrc= Interlocked.CompareExchange<CancellationTokenSource>(ref this.cts, null, this.cts);
        if (null == ctSrc) return;
        ctSrc.Cancel();
        ctSrc.Dispose();
        GC.SuppressFinalize(this);
      }
    }
  }
}