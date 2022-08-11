using System;
using System.Diagnostics;

namespace Tlabs.Misc {

  ///<summary>Simplified <see cref="Stopwatch"/> struct.</summary>
  ///<example>
  /// var watch= TimingWatch.StartTiming();
  /// ...
  /// var ms= watch.GetElapsedTime().TotalMilliseconds;
  ///</example>
  public struct TimingWatch {
    private static readonly double TICKS= TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    readonly long startTime;

    ///<summary>true if created by <see cref="TimingWatch.StartTiming()"/>.</summary>
    public bool IsStarted => 0 != startTime;

    private TimingWatch(long startTime) {
      this.startTime= startTime;
    }

    ///<summary>Start a new timing.</summary>
    public static TimingWatch StartTiming() => new TimingWatch(Stopwatch.GetTimestamp());

    ///<summary>Returns the <see cref="TimeSpan"/> since <see cref="TimingWatch.StartTiming()"/>.</summary>
    public TimeSpan GetElapsedTime() {
      // Is default(ChronoWatch)??
      if (!IsStarted) throw new InvalidOperationException("An uninitialized, or 'default', ChronoWatch cannot be used to get elapsed time.");

      var dt= Stopwatch.GetTimestamp() - startTime;
      return new TimeSpan((long)(TICKS * dt));
    }

    ///<summary>Returns the elapsed milliseconds since <see cref="TimingWatch.StartTiming()"/>.</summary>
    public double GetElapsedMilliseconds() => GetElapsedTime().TotalMilliseconds;
  }

}