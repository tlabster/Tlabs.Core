using System;
using System.Globalization;
using Xunit;

namespace Tlabs.Timing.Test {
  public class TstTimeEnvironment : IDisposable {
    public TstTimeEnvironment() {
      App.Setup= App.Setup with { TimeInfo= new DateTimeHelper(TimeZoneInfo.Local) };
    }
    public void Dispose() => App.Setup= Config.ApplicationSetup.Default;
  }



  [Collection("App.Setup")]
  public class ScheduleTimeTests {
    private TstTimeEnvironment appTimeEnv;

    public ScheduleTimeTests(TstTimeEnvironment appTimeEnv) {
      this.appTimeEnv= appTimeEnv;
    }

    [Fact]
    //[ExpectedException(typeof(InvalidOperationException))]
    public void IScheduleTimeTest() {
      var timePattern= "2011-04-13 17:00:01";
      var expectedSchedule= ParseTime(timePattern);
      ITimePlan stime= new ScheduleTime(timePattern, null);
      var firstSchedule= stime.DueDate((expectedSchedule.AddMilliseconds((double)-TimeScheduler.RESOLUTION_MSEC)));
      Assert.Equal(expectedSchedule, firstSchedule);
      var nextSchedule= stime.DueDate(firstSchedule.AddMilliseconds(TimeScheduler.RESOLUTION_MSEC));
      Assert.True(stime.DueDate(firstSchedule).CompareTo(expectedSchedule) > 0);;
    }

    [Fact]
    //[ExpectedException(typeof(InvalidOperationException))]
    public void NextScheduleTimeTest() {
      var fromNow= new DateTime(2011, 04, 12, 19, 35, 49);
      var timePattern= "2011-04-13 17:00:01";
      var expectedSchedule= ParseTime(timePattern);
      var next= ScheduleTime.NextFrom(fromNow, timePattern);
      Assert.Equal(expectedSchedule, next);

      timePattern= "2011-04-* 17:00:01";
      next= ScheduleTime.NextFrom(fromNow, timePattern);
      Assert.Equal(expectedSchedule, next);

      timePattern= "*-*-* 17:00:01";
      next= ScheduleTime.NextFrom(fromNow, timePattern);
      Assert.Equal(expectedSchedule, next);

      timePattern= "2011-05-* 17:00:01";
      expectedSchedule= ParseTime("2011-05-01 17:00:01");
      next= ScheduleTime.NextFrom(fromNow, timePattern);
      Assert.Equal(expectedSchedule, next);


      fromNow= new DateTime(2009, 04, 12, 19, 35, 49);
      timePattern= "*-02-29 17:00:01";
      expectedSchedule= ParseTime("2012-02-29 17:00:01"); //2012 is a leap year
      next= ScheduleTime.NextFrom(fromNow, timePattern);
      Assert.Equal(expectedSchedule, next);

      timePattern= "*-04-13 17:00:01 | Friday";
      expectedSchedule= ParseTime("2012-04-13 17:00:01"); //April 13, 2012 is a friday
      next= ScheduleTime.NextFrom(fromNow, timePattern);
      Assert.Equal(expectedSchedule, next);
    }

    [Fact]
    public void ScheduleTimePatternTest() {
      var fromNow= new DateTime(2011, 04, 12, 19, 35, 49);
      var scheduleTimePattern= "*-04-13 17:00:01|  " + (int)DayOfWeek.Friday;
      var expectedSchedule= ParseTime("2012-04-13 17:00:01"); //April 13, 2012 is a friday
      var schTime= new ScheduleTime(scheduleTimePattern);
      var next= schTime.DueDate(fromNow);
      Assert.Equal(expectedSchedule, next);
    }


    static DateTime ParseTime(string timeStr) {
      return DateTime.ParseExact(timeStr, TimeScheduler.TIME_FORMAT, DateTimeFormatInfo.InvariantInfo);
    }
  }

}