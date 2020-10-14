using System;
using System.Globalization;

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

    /// <summary>Default Ctor creating a local <see cref="IAppTime"/>.</summary>
    public DateTimeHelper() : this(TimeZoneInfo.Local) { }

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
      if(dt.Kind == DateTimeKind.Unspecified) { return dt; }
      var srcTZ= (dt.Kind == DateTimeKind.Local) ? TimeZoneInfo.Local : TimeZoneInfo.Utc;
      return TimeZoneInfo.ConvertTime(dt, srcTZ, TZinfo);    //explicitly specify source and dest. time zones !!!
    }

    /// <inherit/>
    public DateTime ToUtc(DateTime dt) {
      if(dt.Kind == DateTimeKind.Utc) { return dt; }
      var srcTZ= (dt.Kind == DateTimeKind.Local) ? TimeZoneInfo.Local : TZinfo;
      return TimeZoneInfo.ConvertTime(dt, srcTZ, TimeZoneInfo.Utc);    //explicitly specify source and dest. time zones !!!
    }
  }

  /// <summary>DateTime extension helper</summary>
  public static class DateTimeHelperExt {

    /// <summary>Strip time from <paramref name="date"/>.</summary>
    public static DateTime StripTime(this DateTime date) {
      return new DateTime(date.Year, date.Month, date.Day);
    }

    /// <summary>Converts an IConvertible into an application DateTime</summary>
    public static DateTime ToAppTime(this IConvertible cv) {
      return App.TimeInfo.ToAppTime(cv.ToDateTime(DateTimeFormatInfo.InvariantInfo));
    }
  }
}
