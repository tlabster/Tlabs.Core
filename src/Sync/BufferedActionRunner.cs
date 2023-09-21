#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Tlabs.Sync {

  ///<summary>Buffered action runner</summary>
  public sealed class BufferedActionRunner : IDisposable {
    static ILogger log= new App.AppLogger<BufferedActionRunner>();
    object sync= new();
    BufferedAction? buffAction;

    ///<summary>Run <paramref name="action"/> at least after <paramref name="bufferTime"/> has been elapsed (or earlier due to previous invocation within <paramref name="bufferTime"/>).</summary>
    ///<remarks>
    ///<p>With multiple invocations buffered  within <paramref name="bufferTime"/> only the last invocation's <paramref name="action"/> is being run.</p>
    ///<p>If a subsequent invocation specifies a <paramref name="bufferTime"/> less than the remaining time due for the first buffered, it is run immediately</p>
    ///</remarks>
    public void Run(int bufferTime, Action action) {
      lock (sync) {
        buffAction??= new BufferedAction(this, bufferTime);
        buffAction.Schedule(action, bufferTime);
      }
    }

    private void runAction(Action action) {
      lock (sync) {
        buffAction?.Dispose();
        buffAction= null;
      }
      try { action(); }
      catch (Exception e) {
        log.LogError(e, "Failed to execute buffered action.");
      }
    }

    ///<inheritdoc/>
    public void Dispose() {
      lock (sync) {
        buffAction?.Dispose();
        buffAction= null;
      }
      GC.SuppressFinalize(this);
    }

    private sealed class BufferedAction : IDisposable {
      DateTime start= DateTime.Now;
      CancellationTokenSource cts= new();
      int delay;
      BufferedActionRunner actionRunner;
      Action? action;

      public BufferedAction(BufferedActionRunner actionRunner, int bufferTime) {
        this.actionRunner= actionRunner;
        this.delay= bufferTime;

        Task.Delay(this.delay, this.cts.Token)
            .ContinueWith(t => {
              var act= this.action;
              if (null != act) actionRunner.runAction(act);
            }, TaskContinuationOptions.NotOnCanceled);
      }

      public void Schedule(Action action, int bufferTime) {
        var act= this.action= action;
        if (   bufferTime >= this.delay
            || bufferTime >= this.delay - (DateTime.Now - this.start).TotalMilliseconds) return; //keep schedule
        actionRunner.runAction(act);  //run immediately
      }

      public void Dispose() {
        this.cts.Cancel();
        this.cts.Dispose();
        GC.SuppressFinalize(this);
      }
    }

  }
}