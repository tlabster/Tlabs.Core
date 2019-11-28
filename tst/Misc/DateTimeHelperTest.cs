using System;
using Xunit;

namespace Tlabs.Misc.Tests {

  public class DateTimeHelperTest {

    [Fact]
    public void CurrentTimeTest() {
      var timeZoneInfo= TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
      var tInfo= new DateTimeHelper(timeZoneInfo);

      DateTime now= tInfo.Now;
      TimeSpan diff= tInfo.TZinfo.GetUtcOffset(now);

      Assert.NotEqual(default(TimeSpan), diff);
      Assert.True(tInfo.ToUtc(now) < now);
      Assert.Equal(tInfo.ToAppTime(tInfo.ToUtc(now)), now);

      //Assumes unspecified kind as local and converts it to apptime correctly
      DateTime time=  DateTime.SpecifyKind(DateTime.Parse("1996-12-19T16:39:57.000000-02:00", null, System.Globalization.DateTimeStyles.RoundtripKind), DateTimeKind.Unspecified);
      var t= tInfo.ToAppTime(time);
      Assert.Equal(t.Hour, 19);
    }
  }
}