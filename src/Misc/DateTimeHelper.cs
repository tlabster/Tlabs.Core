using System;

namespace Tlabs {

  /*  TODO: Refactor DateTimeHelper into IAppTime
   *        Also consider to return IAppTime from AppGlobals in place of AppGlobals.TZinfo.
   */

  ///<summary>Application time-zone specific time helper.</summary>
  public interface IAppTime {
    ///<summary>Current time in application time-zone</summary>
    DateTime Now { get; }

    ///<summary>Convert a UTC <paramref name="dt"/> into application time-zone <see cref="DateTime"/>.</summary>
    DateTime ToAppTime(DateTime dt);

    ///<summary>Convert application time-zone <paramref name="dt"/> into a UTC <see cref="DateTime"/>.</summary>
    DateTime ToUtc(DateTime dt);

    ///<summary>Current time in application time-zone</summary>
    TimeZoneInfo TZinfo { get; }
  }

  /// <summary>DateTime Helper implementation</summary>
  public class DateTimeHelper : IAppTime {
    private TimeZoneInfo tzInfo;

    /// <summary>
    /// Constructor from TimeZoneInfo
    /// </summary>
    /// <param name="tzInfo">TimeZone information</param>
    public DateTimeHelper(TimeZoneInfo tzInfo) {
      this.tzInfo= tzInfo;
    }

    /// <inherit/>
    public TimeZoneInfo TZinfo => tzInfo;

    /// <inherit/>
    public DateTime Now => TimeZoneInfo.ConvertTime(DateTime.UtcNow, TZinfo);

    /// <inherit/>
    public DateTime ToAppTime(DateTime dt) {
      return TimeZoneInfo.ConvertTime(dt, TimeZoneInfo.Utc, TZinfo);    //explicitly specify source and dest. time zones !!!
    }

    /// <inherit/>
    public DateTime ToUtc(DateTime dt) {
      return TimeZoneInfo.ConvertTime(dt, TZinfo, TimeZoneInfo.Utc);    //explicitly specify source and dest. time zones !!!
    }
  }

  /// <summary>DateTime extension helper</summary>
  public static class DateTimeHelperExt {

    /// <summary>Strip time from <paramref name="date"/>.</summary>
    public static DateTime StripTime(this DateTime date) {
      return new DateTime(date.Year, date.Month, date.Day);
    }
  }
}
