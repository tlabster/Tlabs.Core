using System;
using Xunit;

namespace Tlabs.Misc.Tests {

  public class DateTimeHelperTest {

    [Fact]
    public void CurrentTimeTest() {
      var timeZoneInfo= TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
      var tInfo= new DateTimeHelper(timeZoneInfo);

      DateTime time0=  DateTime.Parse("1996-12-19T16:39:57.000000-02:00", null, System.Globalization.DateTimeStyles.RoundtripKind);
      TimeSpan diff= tInfo.TZinfo.GetUtcOffset(time0);

      Assert.NotEqual(default(TimeSpan), diff);
      Assert.Equal(18, tInfo.ToUtc(time0).Hour);

      DateTime local=  DateTime.SpecifyKind(time0, DateTimeKind.Local);
      Assert.Equal(18, tInfo.ToUtc(local).Hour);
      Assert.Equal(19, tInfo.ToAppTime(local).Hour);

      //Assumes unspecified kind as local and converts it to apptime correctly
      DateTime time=  DateTime.SpecifyKind(time0, DateTimeKind.Unspecified);
      Assert.Equal(18, tInfo.ToUtc(time).Hour);
      Assert.Equal(19, tInfo.ToAppTime(time).Hour);

      DateTime utc= DateTime.UtcNow;
      Assert.Equal(tInfo.ToUtc(utc).Hour, utc.Hour);
    }
  }
}