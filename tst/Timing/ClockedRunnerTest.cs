using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Tlabs.Sync;
using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Timing.Test {

  public class ClockedRunnerTest {
    ITestOutputHelper tstout;
    
    public ClockedRunnerTest(ITestOutputHelper tstout) { this.tstout= tstout; }
    
    [Fact]
    //[ExpectedException(typeof(InvalidOperationException))]
    public void BasicTest() {
      var n= 3;
      var ival= 100;
      var mon= new SyncMonitor<bool>();
      var cnt= 0;
      Stopwatch timer= new();
      timer.Start();

      var clock= new ClockedRunner("test runner", ival, ctk => {
        tstout.WriteLine($"{cnt}: {timer.ElapsedMilliseconds - cnt*ival} msec");
        if (++cnt >= n) {
          mon.SignalPermanent(true);
          return true;
        }
        return false;
      });
      mon.WaitForSignal();

      Assert.Equal(n, cnt);
      clock.Dispose();
    }

    [Fact]
    public void TimeoutTest() {
      var mon= new SyncMonitor<bool>();
      var cnt= 0;

      var clock= new ClockedRunner("timeout runner", 100, ctk => {
        tstout.WriteLine($"timing out");
        Task.Delay(200).GetAwaiter().GetResult();
        ++cnt;
        return false;
      });

      Task.Delay(400).GetAwaiter().GetResult();
      clock.Dispose();
      Assert.Equal(1, cnt);   //first run timedout
    }

    [Fact]
    public void CancelTest() {
      var cts= new CancellationTokenSource();
      var mon= new SyncMonitor<bool>();
      var cnt= 0;

      var clock= new ClockedRunner("timeout runner", 100, ctk => {
        if (++cnt >= 2) {
          tstout.WriteLine($"Request for cancel on: {cnt}");
          mon.SignalPermanent(true);
        }
        return false;
      }, cts.Token);

      mon.WaitForSignal();
      cts.Cancel();

      Task.Delay(100).GetAwaiter().GetResult();
      Assert.Equal(2, cnt);   //canceled after 2
      clock.Dispose();
    }

    [Fact]
    public void DisposeTest() {
      var mon= new SyncMonitor<bool>();
      var cnt= 0;

      var clock= new ClockedRunner("timeout runner", 100, ctk => {
        if (++cnt >= 2) {
          tstout.WriteLine($"Request for dispose on: {cnt}");
          mon.SignalPermanent(true);
        }
        return false;
      });

      mon.WaitForSignal();
      clock.Dispose();

      Task.Delay(100).GetAwaiter().GetResult();
      Assert.Equal(2, cnt);   //disposed after 2
    }
  }
}