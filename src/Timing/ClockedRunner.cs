using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Tlabs.Timing {


  ///<summary>Utillity to invoke a delegate repeatingly with a given clock interval.</summary>
  public sealed class ClockedRunner : IDisposable {
    static readonly ILogger log= Tlabs.App.Logger<ClockedRunner>();
 
    readonly Stopwatch timer= new();
    readonly CancellationTokenSource cts;
    bool isDisposed;

    ///<summary>Ctor to start the clocked runner.</summary>
    ///<param name="runnerTitle">Diagnostic title of the runner</param>
    ///<param name="interval">clocked run inteval in msec.</param>
    ///<param name="run">delegate to run, takes <paramref name="ctk"/> and indicates with a return value == true to abort further clocked invocations.</param>
    ///<param name="ctk"><see cref="CancellationToken"/> to cancel the clocked runner</param>
    ///<remarks>The called <paramref name="run"/> delegate must return within <paramref name="interval"/> msec.
    /// Failing to do so will result into a <see cref="TimeoutException"/>
    ///</remarks>
    public ClockedRunner(string runnerTitle, long interval, Func<CancellationToken, bool> run, CancellationToken ctk= default) {
      this.cts= CancellationTokenSource.CreateLinkedTokenSource(ctk);
      _= startClockedRunner(runnerTitle, interval, run);
    }

    private async Task startClockedRunner(string runnerTitle, long interval, Func<CancellationToken, bool> run) {
      try {
        log.LogInformation("Starting clocked running of: {title}...", runnerTitle);
        timer.Start();
        for (long t= 0; !cts.Token.IsCancellationRequested;) {
          t+= interval;
          var untilNext= (int)(t - timer.ElapsedMilliseconds);
          if (untilNext < 0) throw new TimeoutException($"Clocked running timed out for: {runnerTitle}");
          await Task.Delay(untilNext, cts.Token);
          if (run(cts.Token)) return;
        }
      }
      catch (Exception e) when (e is not TaskCanceledException) {
        log.LogError("Error on clocked running: {title}", e);
      }
      finally {
        log.LogInformation("Stopped clocked running of: {title}", runnerTitle);
        this.Dispose();
      }
    }

    ///<summary>Stopps the clocked runner and release of all resources.</summary>
    public void Dispose() {
      if (isDisposed) return;
      cts.Cancel();
      cts.Dispose();
      isDisposed= true;
    }
  }
}