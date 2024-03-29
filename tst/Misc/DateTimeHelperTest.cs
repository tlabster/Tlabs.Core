﻿using System;
using Xunit;

namespace Tlabs.Misc.Tests {

  [Collection("App.Setup")]
  public class DateTimeHelperTest {

    [Fact]
    public void CurrentTimeTest() {
      App.Setup= App.Setup with { TimeInfo= Config.ApplicationSetup.Default.TimeInfo };
      Assert.Equal(DateTimeKind.Utc, App.Setup.TimeInfo.Now.Kind);

      var tzid=   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
               ? "W. Europe Standard Time"  //windows
               : "Europe/Berlin";           //non windows

      var timeZoneInfo= TimeZoneInfo.FindSystemTimeZoneById(tzid);
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
  }
}