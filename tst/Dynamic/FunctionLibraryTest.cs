
using System;
using System.Collections.Generic;
using Xunit;

namespace Tlabs.Dynamic.Misc.Tests {
  [Collection("MemoryDB")]
  public class FunctionLibraryTest {
    [Fact]
    void AgeAtTest() {
      var now= DateTime.Now;
      var dateAt= new DateTime(2018, 10, 5);
      var date= now.AddYears(-28);
      Assert.Equal(28, Function.AgeAt(date, null));
      date= new DateTime(1990, 05, 01);
      Assert.Equal(28, Function.AgeAt(date, dateAt));
      Assert.Equal(0, Function.AgeAt(null, DateTime.Now));
      Assert.Equal(38, Function.AgeAt(date, dateAt.AddYears(10)));
    }

    [Fact]
    void YearsDiffTest() {
      var date1= new DateTime(1985, 12, 21);
      var date2= new DateTime(1990, 12, 20);
      Assert.Equal(4, Function.YearsDiff(date2, date1));
      Assert.Equal(4, Function.YearsDiff(date1, date2));
    }

    [Fact]
    void MonthsDiffTest() {
      var date1= new DateTime(1990, 01, 01, 0, 0, 0);
      var date2= new DateTime(1991, 01, 01, 23, 59, 59);
      Assert.Equal(12, Function.MonthsDiff(date1, date2));
      Assert.Equal(12, Function.MonthsDiff(date2, date1));
    }

    [Fact]
    void DaysDiffTest() {
      var date1= new DateTime(1991, 01, 01, 23, 59, 58);
      var date2= new DateTime(1991, 01, 04, 0, 0, 0);
      Assert.Equal(2, Function.DaysDiff(date1, date2));
      Assert.Equal(2, Function.DaysDiff(date2, date1));
    }

    [Fact]
    void AfterDaysTest() {
      var date1= new DateTime(2018, 12, 09);
      Assert.Equal(5, Function.DaysDiff(date1, Function.AfterDays(date1, 5)));
      //If it falls in a sunday one day is added on top
      Assert.Equal(8, Function.DaysDiff(date1, Function.AfterDays(date1, 7)));
    }

    [Fact]
    void AtMostOneTest() {
      var array = new bool[] { false, false };
      Assert.False(Function.AtMostOne(array));

      array = new bool[] { true, false, false };
      Assert.True(Function.AtMostOne(array));

      array = new bool[] { true, true, false };
      Assert.False(Function.AtMostOne(array));
    }

    [Fact]
    void ChooseTest() {
      var array = new string[] { null, null, "A", "B" };
      Assert.Equal("A", Function.Choose(array));

      array = new string[] { null, null };
      Assert.Null(Function.Choose(array));

      array = new string[] { };
      Assert.Null(Function.Choose(array));
    }

    [Fact]
    void RecentTest() {
      var date1= new DateTime(1991, 01, 01, 23, 59, 59);
      var date2= new DateTime(1991, 01, 02, 0, 0, 0);
      Assert.Equal(date2, Function.Recent(date1, date2));
      Assert.Equal(date2, Function.Recent(date2, date1));
      Assert.Equal(date2, Function.Recent("19002191", date2));
    }

    [Fact]
    void RecentDateTest() {
      var date1= new DateTime(1991, 01, 01, 23, 59, 59);
      var date2= new DateTime(1991, 01, 02, 0, 0, 0);
      Assert.Equal(date2, Function.RecentDate(date1, date2));
      Assert.Equal(date2, Function.RecentDate(date2, date1));
    }

    [Fact]
    void FormerTest() {
      var date1= new DateTime(1991, 01, 01, 23, 59, 59);
      var date2= new DateTime(1991, 01, 02, 0, 0, 0);
      Assert.Equal(date1, Function.Former(date1, date2));
      Assert.Equal(date1, Function.Former(date2, date1));
      Assert.Equal(date2, Function.Former("20182191", date2));
    }

    [Fact]
    void FormerDateTest() {
      var date1= new DateTime(1991, 01, 01, 23, 59, 59);
      var date2= new DateTime(1991, 01, 02, 0, 0, 0);
      Assert.Equal(date1, Function.FormerDate(date1, date2));
      Assert.Equal(date1, Function.FormerDate(date2, date1));
    }

    [Fact]
    void WhenRecentTest() {
      var date1= new DateTime(1991, 01, 01, 23, 59, 59);
      var date2= new DateTime(1991, 01, 02, 0, 0, 0);
      Assert.Null(Function.WhenRecent(date1, date2));
      Assert.Equal(date2, Function.WhenRecent(date2, date1));
    }

    [Fact]
    void IsTest() {
      Assert.True(Function.Is(21.0m));
      Assert.True(Function.Is("test"));
      Assert.False(Function.Is(null));
    }


    [Fact]
    void FalseTest() {
      Assert.True(Function.False(false));
      Assert.False(Function.False(null));
    }

    [Fact]
    void NumTest() {
      Assert.Equal(21.0m, Function.Num(21.0m));
      Assert.Equal(0, Function.Num(21));
      Assert.Equal(0, Function.Num(null));
      Assert.Equal(0, Function.Num("test"));
    }

    [Fact]
    void DateTest() {
      Assert.Equal(default(DateTime), Function.Date(null));
      Assert.Equal(default(DateTime), Function.Date("2018-01-01"));
      var dt = DateTime.Now;
      Assert.Equal(dt, Function.Date(dt));
    }

    [Fact]
    void ListTest() {
      Assert.Null(Function.List(null));
      Assert.Null(Function.List("not a list"));
      var list= new List<string> { "test1", "test2" };
      Assert.Equal(2, Function.List(list).Count);
      Assert.Contains("test2", Function.List(list));
      var list2= new List<int> { 1, 3, 4 };
      Assert.Equal(3, Function.List(list2).Count);
      var list3= new List<DateTime> { App.TimeInfo.Now };
      Assert.Single(Function.List(list3));
    }

    // [Fact]
    // void AterDaysTest() {
    //   var date1= new DateTime(1985, 12, 21);
    //   var date2= new DateTime(1990, 12, 20);
    //   Assert.Equal(1, Function.DaysDiff(date1, date2));
    //   Assert.Equal(1, Function.DaysDiff(date2, date1));
    // }
  }
}
