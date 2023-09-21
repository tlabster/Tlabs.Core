using System;
using System.Threading.Tasks;

using Tlabs.Misc;
using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Sync.Test {

  public class BufferedActionRunnerTest {
    ITestOutputHelper tstout;
    
    public BufferedActionRunnerTest(ITestOutputHelper tstout) { this.tstout= tstout; }
    
    class TstAction {
      ITestOutputHelper tstout;
      int n;
      public TimingWatch Timer= TimingWatch.StartTiming();
      public SyncMonitor<bool> Mon= new();
      public int Cnt;

      public TstAction(ITestOutputHelper tstout, int n= 1) {
        this.tstout= tstout;
        this.n= n;
      }
      public void Action() {
        ++Cnt;
        tstout.WriteLine($"{Cnt}: {Timer.ElapsedMilliseconds} msec");
        if (Cnt >= n) Mon.SignalPermanent(true);
      }
    }

    private void asyncRun(Action action) {
      Task.Factory.StartNew(() => action(), TaskCreationOptions.LongRunning);
    }

    [Fact]
    //[ExpectedException(typeof(InvalidOperationException))]
    public void SingelRunTest() {
      using var bufRunner= new BufferedActionRunner();
      var test= new TstAction(tstout);

      bufRunner.Run(50, () => test.Action());
      test.Mon.WaitForSignal();

      Assert.Equal(1, test.Cnt);
    }

    [Fact]
    public void MultiRunTest() {
      using var bufRunner= new BufferedActionRunner();
      var test= new TstAction(tstout);

      asyncRun(() => bufRunner.Run(50, () => test.Action()));
      asyncRun(() => bufRunner.Run(50, () => test.Action()));
      asyncRun(() => bufRunner.Run(50, () => test.Action()));
      asyncRun(() => {
        Task.Delay(10).GetAwaiter().GetResult();
        bufRunner.Run(50, () => test.Action());
      });
      test.Mon.WaitForSignal();

      Assert.Equal(1, test.Cnt);
    }

    [Fact]
    public void OverDueRunTest() {
      using var bufRunner= new BufferedActionRunner();
      var test= new TstAction(tstout, 2);

      while (test.Cnt < 2) {
        asyncRun(() => bufRunner.Run(50, () => test.Action()));
        asyncRun(() => {
          Task.Delay(10).GetAwaiter().GetResult();
          bufRunner.Run(50, () => test.Action());
        });
        Task.Delay(10).GetAwaiter().GetResult();
        bufRunner.Run(5, () => test.Action());
      }
      test.Mon.WaitForSignal();
      Assert.True(test.Cnt > 1);
    }

    [Fact]
    public void DisposeTest() {
      using var bufRunner= new BufferedActionRunner();
      var test= new TstAction(tstout, 2);

      bufRunner.Run(50, () => test.Action());
      bufRunner.Run(50, () => test.Action());
      bufRunner.Run(50, () => test.Action());
      bufRunner.Dispose();
      Task.Delay(100).GetAwaiter().GetResult();

      Assert.True(test.Cnt == 0);
    }

  }
}