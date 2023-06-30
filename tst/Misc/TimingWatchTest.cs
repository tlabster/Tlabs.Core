using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Misc.Test {

  public class TimingWatchTest {
    ITestOutputHelper tstout;
    
    public TimingWatchTest(ITestOutputHelper tstout) { this.tstout= tstout; }
    
    [Fact]
    //[ExpectedException(typeof(InvalidOperationException))]
    public void TimingTest() {
      Stopwatch watch= new();
      watch.Start();
      var timer= TimingWatch.StartTiming();

      Task.Delay(10).GetAwaiter().GetResult();

      var elapsed= timer.ElapsedMilliseconds;
      var elpTime= timer.GetElapsedTime();
      Assert.True(2 > Math.Abs(watch.ElapsedMilliseconds - elapsed));
      Assert.True(1 > Math.Abs(elapsed - elpTime.TotalMilliseconds));
      // tstout.WriteLine($"Elapsed: {elapsed:D}");
    }

  }
}