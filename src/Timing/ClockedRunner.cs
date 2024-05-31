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
    int isDisposed;

    ///<summary>Clocked runner title.</summary>
    public string Title { get; }

    ///<summary>Clocked runner interval.</summary>
    public long ClockInterval { get; }

    ///<summary>Ctor to start the clocked runner.</summary>
    ///<param name="runnerTitle">Diagnostic title of the runner</param>
    ///<param name="interval">clocked run inteval in msec.</param>
    ///<param name="run">delegate to run, takes <paramref name="ctk"/> and indicates with a return value == true to abort further clocked invocations.</param>
    ///<param name="ctk"><see cref="CancellationToken"/> to cancel the clocked runner</param>
    ///<remarks>The called <paramref name="run"/> delegate must return within <paramref name="interval"/> msec.
    /// Failing to do so will result into a <see cref="TimeoutException"/>
    ///</remarks>
    public ClockedRunner(string runnerTitle, long interval, Func<CancellationToken, bool> run, CancellationToken ctk= default) {
      this.Title= runnerTitle;
      this.ClockInterval= interval;
      this.cts= CancellationTokenSource.CreateLinkedTokenSource(ctk, App.AppLifetime.ApplicationStopping);
      _= startClockedRunner(run);
    }

    private async Task startClockedRunner(Func<CancellationToken, bool> run) {
      try {
        log.LogInformation("Starting clocked running of: {title}...", Title);
        timer.Start();
        for (long t= 0; !cts.Token.IsCancellationRequested;) {
          t+= ClockInterval;
          var untilNext= (int)(t - timer.ElapsedMilliseconds);
          while (untilNext < 0) {
            for (long l= 0, n= -untilNext / ClockInterval; l < n+1; ++l ) {
              if (cts.Token.IsCancellationRequested || run(cts.Token)) return;    //catch up missed invocations
              t+= ClockInterval;
              Task.Yield().GetAwaiter().GetResult();    //yield to other tasks
            }
            untilNext= (int)(t - timer.ElapsedMilliseconds);
          }
          await Task.Delay(untilNext, cts.Token);
          if (cts.Token.IsCancellationRequested || run(cts.Token)) return;
        }
      }
      catch (Exception e) when (e is not TaskCanceledException) {
        log.LogError("Error on clocked running: {title}", e);
      }
      finally {
        log.LogInformation("Stopped clocked running of: {title}", Title);
        this.Dispose();
      }
    }

    ///<summary>Stopps the clocked runner and release of all resources.</summary>
    public void Dispose() {
      if (0 != Interlocked.Exchange(ref isDisposed, 1)) return;
      cts.Cancel();
      cts.Dispose();
    }
  }
}