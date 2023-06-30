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
    static readonly double TPS= TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    static readonly long TPMS= Stopwatch.Frequency / 1000;
    readonly long startTime;

    ///<summary>true if created by <see cref="TimingWatch.StartTiming()"/>.</summary>
    public bool IsStarted => 0 != startTime;

    private TimingWatch(long startTime) {
      this.startTime= startTime;
    }

    ///<summary>Start a new timing.</summary>
    public static TimingWatch StartTiming() => new TimingWatch(Stopwatch.GetTimestamp());

    ///<summary>Returns the <see cref="TimeSpan"/> since <see cref="TimingWatch.StartTiming()"/>.</summary>
    public TimeSpan GetElapsedTime() => new TimeSpan((long)(ElapsedTicks));

    ///<summary>Returns the elapsed milliseconds since <see cref="TimingWatch.StartTiming()"/>.</summary>
    public double GetElapsedMilliseconds() => GetElapsedTime().TotalMilliseconds;

    ///<summary>Returns the elapsed ticks since <see cref="TimingWatch.StartTiming()"/>.</summary>
    public long ElapsedTicks { get {
      if (!IsStarted) throw new InvalidOperationException($"An uninitialized, or 'default', {nameof(TimingWatch)} cannot be used to get elapsed time.");
      return (Stopwatch.GetTimestamp() - startTime);
    }}

    ///<summary>Returns the elapsed milliseconds since <see cref="TimingWatch.StartTiming()"/>.</summary>
    public long ElapsedMilliseconds => ElapsedTicks / TPMS;

  }

}