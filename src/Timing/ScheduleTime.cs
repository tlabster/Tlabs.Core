using System;
using System.Collections.Generic;
using System.Globalization;

namespace Tlabs.Timing {

  ///<summary>Basic <see cref="ITimePlan"/> implementation to setup a time schedule on simple time pattern syntax.</summary>
  ///<remarks>
  ///<para>The schedule time pattern is based on this time format:</para>
  ///<code>  yyyy-mm-dd hh:mm:ss { | Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday }</code>
  ///where the part in { } braces specifying day-of-week constrains is optional.
  ///<para>Each integer component in the date-time part could be replaced with a '*' to denote a wildcard.</para>
  ///</remarks>
  ///<example>
  ///<list type="table">
  ///<item>
  ///<term>2017-10-25 10:35:27</term>
  ///<description>exact not repeating time as given</description>
  ///</item>
  ///<item>
  ///<term>2017-10-* 10:35:27 | Monday</term>
  ///<description>every Monday of October, 2017 at 10:35:27</description>
  ///</item>
  ///<item>
  ///<term>*-*-* 10:35:27 | Sunday, Saturday</term>
  ///<description>every weekend at 10:35:27</description>
  ///</item>
  ///</list>
  ///</example>
  public sealed class ScheduleTime : ITimePlan {
    const string TIME_TMPL= "yyyy-mm-dd hh:mm:ss";
    const string PAT_WILDCARD= "*";
    static readonly char[] TIME_PAT_SEP= new char[] { '-', ' ', ':' };
    static readonly int PAT_COMP_COUNT= TIME_TMPL.Split(TIME_PAT_SEP).Length;
    // static readonly int[] PAT_CHK_POINTS= new int[] { 4, 7, 10, 13, 16 };
    // static readonly char[] PAT_SEP_POINTS= new char[] { '-', '-', ' ', ':', ':' };
    static readonly int[] TIME_COMP_BASE= new int[] { 1, 1, 1, 0, 0, 0 };
    static readonly int[] TIME_COMP_MAX= new int[] { int.MaxValue, 12, -1, 23, 59, 59 };
    static readonly Calendar CAL= DateTimeFormatInfo.InvariantInfo.Calendar;

    ///<summary>Next time schedule <paramref name="fromNow"/> defined by <paramref name="scheduleTimePattern"/></summary>
    ///<remarks>(See class for schedule-time pattern.)</remarks>
    public static DateTime NextFrom(DateTime fromNow, string scheduleTimePattern) {
      return new ScheduleTime(scheduleTimePattern).DueDate(fromNow);
    }

    DateTime fromNow;
    string[] patComp;
    IList<DayOfWeek> weekDays;
    readonly int[] timeComp= new int[PAT_COMP_COUNT];

    ///<summary>Ctor from <paramref name="scheduleTimePattern"/></summary>
    ///<remarks>(See class for schedule-time pattern.)</remarks>
    public ScheduleTime(string scheduleTimePattern) {
      if (null == scheduleTimePattern) throw new ArgumentNullException(nameof(scheduleTimePattern));
      var parts= scheduleTimePattern.Split('|');
      if (parts.Length > 2) throw new ArgumentException($"Invalid schedule time pattern: '{scheduleTimePattern}'");
      var wDays= new List<DayOfWeek>();
      var days= 2 == parts.Length ? parts[1].Split(',') : Array.Empty<string>();
      foreach (var no in days)
        wDays.Add((DayOfWeek)Enum.Parse(typeof(DayOfWeek), no.Trim()));

      Init(parts[0].Trim(), wDays);
    }

    ///<summary>Ctor from <paramref name="timePattern"/> and <paramref name="weekDays"/>.</summary>
    ///<remarks>(See class for time pattern.)</remarks>
    public ScheduleTime(string timePattern, IList<DayOfWeek> weekDays) { Init(timePattern, weekDays); }

    private void Init(string timePattern, IList<DayOfWeek> weekDays) {
      //time pattern format is: "yyyy-mm-dd hh:mm:ss"
      if (null == timePattern) throw new ArgumentNullException(nameof(timePattern));
      if (PAT_COMP_COUNT != (patComp= timePattern.Split(TIME_PAT_SEP)).Length)
        throw new ArgumentException($"Invalid schedule time pattern: '{timePattern}'");

      this.weekDays= weekDays;
      try { DueDate(App.TimeInfo.Now); } //validate timePattern
      catch (FormatException) {
        throw new ArgumentException($"Invalid schedule time pattern: '{timePattern}'");
      }
    }

    ///<inheritdoc/>
    public DateTime DueDate(DateTime fromNow) {
      this.fromNow= fromNow;
      TIME_COMP_BASE.CopyTo(timeComp, 0);
      IList<int> wildPos= new List<int>();
      var nowComp= new int[] { fromNow.Year, fromNow.Month, fromNow.Day, fromNow.Hour, fromNow.Minute, fromNow.Second };

      for (int l = 0; l < PAT_COMP_COUNT; ++l) {
        if (PAT_WILDCARD == patComp[l]) {
          wildPos.Add(l);
          timeComp[l]= nowComp[l];
          if (false == Time_NOT_Next(DateTimeFromComponents))
            timeComp[l]= TIME_COMP_BASE[l];
        }
        else
          timeComp[l]= int.Parse(patComp[l], NumberFormatInfo.InvariantInfo);
      }

      var nextTime= DateTimeFromComponents;
      if (Time_NOT_Next(nextTime)) {
        nextTime= DateTime.MinValue;
        for (int l = wildPos.Count-1; l >= 0 && Time_NOT_Next(nextTime); --l) {
          var wildIdx= wildPos[l];
          do {
            if (AdjComp(wildIdx)) break;
            nextTime= DateTimeFromComponents;
          }
          while (Time_NOT_Next(nextTime));
        }
      }

      return (nextTime <= fromNow) ? DateTime.MaxValue : nextTime;
    }


    private DateTime DateTimeFromComponents {
      get {
        try { return new DateTime(timeComp[0], timeComp[1], timeComp[2], timeComp[3], timeComp[4], timeComp[5]); }
        catch (ArgumentOutOfRangeException) {
          return DateTime.MinValue;
        }
      }
    }

    private bool Time_NOT_Next(DateTime time) {
      if (time <= fromNow) return true;
      /* Check for day-of-week restriction:
      */
      return (null != weekDays && 0 != weekDays.Count && false == weekDays.Contains(time.DayOfWeek));
    }

    private bool AdjComp(int l) {
      var max= l == 2 ? CAL.GetDaysInMonth(timeComp[0], timeComp[1]) : TIME_COMP_MAX[l];
      var overrun= (++timeComp[l] > max);
      if (overrun) timeComp[l]= TIME_COMP_BASE[l];
      return overrun;
    }

  }
}