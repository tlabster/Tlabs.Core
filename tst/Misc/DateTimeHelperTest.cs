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
    }
  }
}