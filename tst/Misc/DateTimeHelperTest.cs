using System;

using Tlabs.Timing.Test;

using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Misc.Tests {

  [Collection("App.Setup")]
  public class DateTimeHelperTest(ITestOutputHelper tstout, TstTimeEnvironment appTimeEnv) {

    [Fact]
    public void BasicTestTest() {
      Assert.NotNull(appTimeEnv);
      Assert.Equal(TimeZoneInfo.Utc, App.TimeInfo.TZinfo);

      var dt= App.TimeInfo.Now;
      Assert.Equal(DateTimeKind.Utc, dt.Kind);
    }

    [Fact]
    public void CurrentTimeTest() {
      App.Setup= App.Setup with { TimeInfo= Config.ApplicationSetup.Default.TimeInfo };
      Assert.Equal(DateTimeKind.Utc, App.Setup.TimeInfo.Now.Kind);

      var timeZoneInfo= CETinfo();
      var tInfo= new DateTimeHelper(timeZoneInfo);

      DateTime time0=  DateTime.Parse("1996-12-19T16:39:57.000000-02:00", null, System.Globalization.DateTimeStyles.RoundtripKind);
      TimeSpan diff= tInfo.TZinfo.GetUtcOffset(time0);

      Assert.NotEqual(default(TimeSpan), diff);
      Assert.Equal(18, tInfo.ToUtc(time0).Hour);

      // DateTime local=  DateTime.SpecifyKind(time0, DateTimeKind.Local);
      Assert.Equal(DateTimeKind.Local, time0.Kind);
      Assert.Equal(18, tInfo.ToUtc(time0).Hour);
      Assert.Equal(19, tInfo.ToAppTime(time0).Hour);

      DateTime utc= DateTime.UtcNow;
      Assert.Equal(tInfo.ToUtc(utc).Hour, utc.Hour);
      IConvertible dateTimeSt= "1996-12-19T16:39:57.000000-02:00";

      Assert.Equal(DateTimeKind.Utc, dateTimeSt.ToAppTime().Kind);
      Assert.Equal(18, dateTimeSt.ToAppTime().Hour);
    }

    static TimeZoneInfo CETinfo() {
      var tzid=   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
               ? "W. Europe Standard Time"  //windows
               : "Europe/Berlin";           //non windows

      return TimeZoneInfo.FindSystemTimeZoneById(tzid);
    }

    [Fact]
    public void DaylightSavingTest() {
      // foreach (var tzi in TimeZoneInfo.GetSystemTimeZones())
      //   tstout.WriteLine($"{tzi.DisplayName} - {tzi.Id} ({tzi.DaylightName})");

      var cet= CETinfo();
      tstout.WriteLine($"\nCET: {cet.DisplayName} - {cet.Id} ({cet.DaylightName})");
      tstout.WriteLine($"serialized CET: {cet.ToSerializedString()}");

      var dt= DateTime.SpecifyKind(DateTime.Parse("2024-03-31T01:01:01"), DateTimeKind.Local);
      tstout.WriteLine($"\nstandard time: {dt:o}");
      Assert.False(cet.IsDaylightSavingTime(dt));

      dt= DateTime.SpecifyKind(DateTime.Parse("2024-03-31T02:01:01"), DateTimeKind.Unspecified);
      tstout.WriteLine($"invalid: {dt:o}");   //daylight saving time started from 02:00 and moved ahead to 03:00
      Assert.True(cet.IsInvalidTime(dt));

      dt= DateTime.SpecifyKind(DateTime.Parse("2024-03-31T03:01:01"), DateTimeKind.Local);
      tstout.WriteLine($"daylight time: {dt:o}");
      Assert.True(cet.IsDaylightSavingTime(dt));
    }

  }
}