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
      var watchElapse= watch.ElapsedMilliseconds;
      var diff= Math.Abs(watchElapse - elapsed);
      if (diff > 2) tstout.WriteLine("watch: {0:D} timer: {1:D} {2:G}", watchElapse, elapsed, elpTime.TotalMilliseconds);
      Assert.True(diff <= 2);
      var ddf= Math.Abs(elapsed - elpTime.TotalMilliseconds);
      if (ddf > 2) tstout.WriteLine("elapsed ms: {0:D} timer elapsed: {1:G}", elapsed, elpTime.TotalMilliseconds);
      //Assert.True(diff <= 1);
    }

  }
}