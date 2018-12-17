using System;
using System.Collections.Generic;
using System.Linq.Expressions;


namespace Tlabs.Dynamic.Internal  {

  /** Validation helper function library
   */
  internal class Function {
    private static readonly Expression<Func<bool, bool, bool>> OneOf2Exp= (b1, b2) => (b1^b2);
    private static readonly Expression<Func<bool, bool, bool, bool>> OneOf3Exp= (b1, b2, b3) => (b1^b2^b3);
    private static readonly Expression<Func<bool, bool, bool, bool, bool>> OneOf4Exp= (b1, b2, b3, b4) => (b1^b2^b3^b4);
    private static readonly Expression<Func<bool, bool, bool, bool, bool, bool>> OneOf5Exp= (b1, b2, b3, b4, b5) => (b1^b2^b3^b4^b5);
    private static readonly Expression<Func<bool, bool, bool, bool, bool, bool, bool>> OneOf6Exp= (b1, b2, b3, b4, b5, b6) => (b1^b2^b3^b4^b5^b6);
    private static readonly Expression<Func<bool, bool, bool>> AtMostOne2Exp= (b1, b2) => AtMostOne(b1, b2);
    private static readonly Expression<Func<bool, bool, bool, bool>> AtMostOne3Exp= (b1, b2, b3) => AtMostOne(b1, b2, b3);
    private static readonly Expression<Func<bool, bool, bool, bool, bool>> AtMostOne4Exp= (b1, b2, b3, b4) => AtMostOne(b1, b2, b3, b4);
    private static readonly Expression<Func<bool, bool, bool, bool, bool, bool>> AtMostOne5Exp= (b1, b2, b3, b4, b5) => AtMostOne(b1, b2, b3, b4, b5);
    private static readonly Expression<Func<bool, bool, bool, bool, bool, bool, bool>> AtMostOne6Exp= (b1, b2, b3, b4, b5, b6) => AtMostOne(b1, b2, b3, b4, b5, b6);
    private static readonly Expression<Func<object, object, object>> Choose2Exp= (c1, c2) => Choose(c1, c2);
    private static readonly Expression<Func<object, object, object, object>> Choose3Exp= (c1, c2, c3) => Choose(c1, c2, c3);
    private static readonly Expression<Func<object, object, object, object, object>> Choose4Exp= (c1, c2, c3, c4) => Choose(c1, c2, c3, c4);
    private static readonly Expression<Func<object, int>> AgeExp= (o) => AgeAt(o, App.TimeInfo.Now);
    private static readonly Expression<Func<object, DateTime?, int>> AgeAtExp= (o, now) => AgeAt(o, now);
    private static readonly Expression<Func<object, object, decimal>> YearsDiffExp= (d1, d2) => YearsDiff(d1, d2);
    private static readonly Expression<Func<object, object, decimal>> MonthsDiffExp= (d1, d2) => MonthsDiff(d1, d2);
    private static readonly Expression<Func<object, object, decimal>> DaysDiffExp= (d1, d2) => DaysDiff(d1, d2);
    private static readonly Expression<Func<object, object, DateTime?>> AfterDaysExp= (d1, d2) => AfterDays(d1, d2);
    private static readonly Expression<Func<object, object, DateTime?>> RecentExp= (d1, d2) => Recent(d1, d2);
    private static readonly Expression<Func<object, object, DateTime?>> FormerExp= (d1, d2) => Former(d1, d2);
    private static readonly Expression<Func<object, object, DateTime?>> WhenRecentExp= (d1, d2) => WhenRecent(d1, d2);
    internal static readonly Expression<Func<object, bool>> IsExp= (p) => Is(p);
    private static readonly Expression<Func<object, bool>> FalseExp= (p) => False(p);
    private static readonly Expression<Func<object, decimal>> NumExp= (o) => Num(o);
    private static readonly Expression<Func<object, object, bool>> HasFlagsExp= (o1, o2) => HasFlags(o1, o2);

    internal static int AgeAt(object o, DateTime? at) {
      var date= o as DateTime?;
      if (!date.HasValue) return 0;
      var now= at.HasValue ? at.Value : App.TimeInfo.Now;
      var age= now.Year - date.Value.Year;
      return date > now.AddYears(-age) ? age-1 : age;
    }
    internal static decimal YearsDiff(object o1, object o2) {
      var dv1= (o1 as DateTime?).GetValueOrDefault();
      var dv2= (o2 as DateTime?).GetValueOrDefault();
      var t= (dv1 - dv2).Ticks;
      var y= dv1.Year -  dv2.Year;
      var diff = t >= 0
             ? dv2.AddYears(y) > dv1 ? y-1 : y
             : dv2.AddYears(y) < dv1 ? y+1 : y;
      return Math.Abs(diff);
    }
    internal static decimal MonthsDiff(object o1, object o2) {
      var dv1= (o1 as DateTime?).GetValueOrDefault();
      var dv2= (o2 as DateTime?).GetValueOrDefault();
      var t= (dv1 - dv2).Ticks;
      var m= (12 * (dv1.Year-1) + dv1.Month-1) - (12 * (dv2.Year-1) + dv2.Month-1);
      var diff =t >= 0
             ? dv2.AddMonths(m) > dv1 ? m-1 : m
             : dv2.AddMonths(m) < dv1 ? m+1 : m;
      return Math.Abs(diff);
    }
    internal static decimal DaysDiff(object o1, object o2) {
      var dv1= (o1 as DateTime?).GetValueOrDefault();
      var dv2= (o2 as DateTime?).GetValueOrDefault();
      var tspan= (dv1 - dv2);
      var totalDays= tspan.TotalDays;
      var daySpan=  totalDays > 0 ? Math.Floor(totalDays) : Math.Ceiling(totalDays);
      return (decimal)Math.Abs(daySpan);
    }
    internal static DateTime? AfterDays(object o1, object od) {
      var nd= o1 as DateTime?;
      if (null == nd) return nd;
      var days= Convert.ToInt32(od as IConvertible ?? 0);
      var dd= nd.Value.AddDays(days);
      if (dd.DayOfWeek == DayOfWeek.Sunday) //TODO: check for more known (bank) holidays
        dd= dd.AddDays(1);
      return dd;
    }
    internal static bool AtMostOne(params bool[] b) {
      int n= b.Length;
      bool bl, res= false;
      for (int l = 0; l < n; ++l) {
        res= res | (bl= b[l]);
        if (res) for (int j = l+1; j < n; ++j)
          if (b[j]) return false;
      }
      return res;
    }

    internal static object Choose(params object[] parms) {
      foreach(var p in parms)
        if (null != p) return p;
      return null;
    }

    internal static DateTime? Recent(object o1, object o2) {
      return RecentDate(o1 as DateTime?, o2 as DateTime?);
    }
    internal static DateTime? RecentDate(DateTime? d1, DateTime? d2) {
      if (!d1.HasValue) return d2;
      if (!d2.HasValue) return d1;
      return d1 > d2 ? d1 : d2;
    }
    internal static DateTime? Former(object o1, object o2) {
      return FormerDate(o1 as DateTime?, o2 as DateTime?);
    }
    internal static DateTime? FormerDate(DateTime? d1, DateTime? d2) {
      if (!d1.HasValue) return d2;
      if (!d2.HasValue) return d1;
      return d1 < d2 ? d1 : d2;
    }
    internal static DateTime? WhenRecent(object o1, object o2) { //return if (d1 > d2) d1; else null;
      var d1= o1 as DateTime?;
      var d2= o2 as DateTime?;
      if (null == d2) return d1;
      if (d1.HasValue && d1 <= d2) return null;
      return d1;
    }
    internal static bool Is(object p) {
      return null != p && !False(p);
    }
    internal static bool False(object p) { 
      var b= p as bool?;
      return b.HasValue && false == b.Value;
    }
    internal static decimal Num(object o) {
      var num= o as Decimal?;
      return num ?? 0;
    }

    internal static bool HasFlags(object o1, object o2) {
      int flags= (o1 as int?) ?? 0;
      int mask= (o2 as int?) ?? 0;
      return 0 != (flags & mask);
    }

    internal static readonly IDictionary<string, object> Library= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
      ["@OneOf2"]= OneOf2Exp,
      ["@OneOf3"]= OneOf3Exp,
      ["@OneOf4"]= OneOf4Exp,
      ["@OneOf5"]= OneOf5Exp,
      ["@OneOf6"]= OneOf6Exp,
      ["@AtMostOne2"]= AtMostOne2Exp,
      ["@AtMostOne3"]= AtMostOne3Exp,
      ["@AtMostOne4"]= AtMostOne4Exp,
      ["@AtMostOne5"]= AtMostOne5Exp,
      ["@AtMostOne6"]= AtMostOne6Exp,
      ["@Choose2"]= Choose2Exp,
      ["@Choose3"]= Choose3Exp,
      ["@Choose4"]= Choose4Exp,
      ["@AgeAt"]= AgeAtExp,
      ["@Age"]= AgeExp,
      ["@YearsDiff"]= YearsDiffExp,
      ["@MonthsDiff"]= MonthsDiffExp,
      ["@DaysDiff"]= DaysDiffExp,
      ["@AfterDays"]= AfterDaysExp,
      ["@Recent"]= RecentExp,
      ["@Former"]= FormerExp,
      ["@WhenRecent"]= WhenRecentExp,
      ["@Is"]= IsExp,
      ["@False"]= FalseExp,
      ["@Num"]= NumExp,
      ["@HasFlags"]= HasFlagsExp
    };
  }
}
