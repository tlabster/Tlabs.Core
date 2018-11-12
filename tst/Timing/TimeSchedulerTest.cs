using System;
using System.Threading;
using Xunit;

namespace Tlabs.Timing.Test {
  [CollectionDefinition("TstTimeScope")]
  public class AppTimeCollection : ICollectionFixture<TstTimeEnvironment> {
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
  }

  [Collection("TstTimeScope")]
  public class TimeSchedulerTest {
    private TstTimeEnvironment appTimeEnv;
    DateTime expectedTime;
    TestScheduleTime schTime1;
    TestScheduleTime schTime2;
    TestScheduleTime schTime3;
    Action dueTimeTestHandler1;
    int testHandlerCnt1;
    Action dueTimeTestHandler2;
    int testHandlerCnt2;
    Action dueTimeTestHandler3;
    int testHandlerCnt3;

    public TimeSchedulerTest(TstTimeEnvironment appTimeEnv) {
      this.dueTimeTestHandler1= DueTimeTestHandler1;
      this.dueTimeTestHandler2= DueTimeTestHandler2;
      this.dueTimeTestHandler3= DueTimeTestHandler3;
      this.appTimeEnv= appTimeEnv;
    }

    [Fact]
    public void BasicTest() {

      var tmScheduler= new TimeScheduler();
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime); //, "must have no time schedule");

      expectedTime= App.TimeInfo.Now.AddSeconds(5);
      schTime1= new TestScheduleTime(expectedTime);
      testHandlerCnt1= 0;
      tmScheduler.Add(schTime1, dueTimeTestHandler1);
      Assert.Equal(expectedTime, tmScheduler.NextDueTime);  //, "expected schTime as next due time");
      tmScheduler.Remove(dueTimeTestHandler1);
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);   //, "must have no time schedule after remove");
      Assert.Equal(0, testHandlerCnt1);   //, "must have invoked no dueTimeCallee");

      var delay= 100;
      expectedTime= App.TimeInfo.Now.AddMilliseconds(delay);
      schTime1= new TestScheduleTime(expectedTime);
      testHandlerCnt1= 0;
      tmScheduler.Add(schTime1, dueTimeTestHandler1);
      Assert.Equal(expectedTime, tmScheduler.NextDueTime);
      schTime1.ScheduleTime= schTime1.ScheduleTime.AddMilliseconds(delay);//reschedule
      Sync.Wait(1, delay+10);
      Assert.Equal(1, testHandlerCnt1);
      Assert.Equal(schTime1.ScheduleTime, tmScheduler.NextDueTime);
      Sync.Wait(1, delay+10);
      Assert.Equal(2, testHandlerCnt1);
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);

      tmScheduler.Remove(dueTimeTestHandler1);
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);


    }

    [Fact]
    public void ScheduleQueueOrderTest() {
      var tmScheduler= new TimeScheduler();
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);   //, "must have no time schedule");

      testHandlerCnt1= testHandlerCnt2= testHandlerCnt3= 0;
      schTime1= new TestScheduleTime(App.TimeInfo.Now.AddYears(1));

      tmScheduler.Add(schTime1, dueTimeTestHandler1);
      tmScheduler.Add(schTime1, dueTimeTestHandler1);
      tmScheduler.Add(schTime1, dueTimeTestHandler1);
      Assert.NotEqual(DateTime.MaxValue, tmScheduler.NextDueTime);  //, "must have valid time schedule");
      tmScheduler.Remove(dueTimeTestHandler1);
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);   //, "must have removed all time schedules");



      var startTime= App.TimeInfo.Now;
      schTime1= new TestScheduleTime(startTime.AddHours(1));
      schTime2= new TestScheduleTime(startTime.AddHours(2));
      schTime3= new TestScheduleTime(startTime.AddHours(3));
      testHandlerCnt1= testHandlerCnt2= testHandlerCnt3= 0;

      tmScheduler.Add(schTime2, dueTimeTestHandler2);
      tmScheduler.Add(schTime1, dueTimeTestHandler1);
      tmScheduler.Add(schTime3, dueTimeTestHandler3);

      Assert.Equal(schTime1.DueDate(startTime), (DateTime)tmScheduler.NextDueTime);   //, "first schedule time nust be schTime1");
      tmScheduler.Remove(dueTimeTestHandler2);
      Assert.Equal(schTime1.DueDate(startTime), (DateTime)tmScheduler.NextDueTime);   //, "first schedule time nust still be schTime1 after remove of dueTimeTestHandler2");
      tmScheduler.Remove(dueTimeTestHandler1);
      Assert.Equal(schTime3.DueDate(startTime), (DateTime)tmScheduler.NextDueTime);   //, "next schedule time nust be schTime3");

      tmScheduler.Remove(dueTimeTestHandler3);
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);   //, "must have no time schedule");
      Assert.Equal(0, testHandlerCnt1);
      Assert.Equal(0, testHandlerCnt2);
      Assert.Equal(0, testHandlerCnt3);

      tmScheduler= new TimeScheduler();
      tmScheduler.Add(schTime1, dueTimeTestHandler1);
      tmScheduler.Add(schTime2, dueTimeTestHandler2);
      tmScheduler.Add(schTime3, dueTimeTestHandler3);
      tmScheduler.Remove(dueTimeTestHandler1);
      Assert.Equal(schTime2.DueDate(startTime), (DateTime)tmScheduler.NextDueTime);   //, "first schedule time nust be schTime2");
      tmScheduler.Remove(dueTimeTestHandler3);
      Assert.Equal(schTime2.DueDate(startTime), (DateTime)tmScheduler.NextDueTime);   //, "first schedule time nust be schTime2");
      tmScheduler.Remove(dueTimeTestHandler2);
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);   //, "must have no time schedule");

    }

    [Fact]
    public void MultipleSchedulesTest() {
      var tmScheduler= new TimeScheduler();
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);   //, "must have no time schedule");

      var startTime= App.TimeInfo.Now;
      schTime1= new TestScheduleTime(startTime.AddMilliseconds(100));
      schTime2= new TestScheduleTime(startTime.AddMilliseconds(200));
      schTime3= new TestScheduleTime(startTime.AddMilliseconds(200));
      testHandlerCnt1= testHandlerCnt2= testHandlerCnt3= 0;

      tmScheduler.Add(schTime3, dueTimeTestHandler3);
      tmScheduler.Add(schTime1, dueTimeTestHandler1);
      tmScheduler.Add(schTime2, dueTimeTestHandler2);
      var t= schTime1.DueDate(startTime);
      Assert.Equal(t, (DateTime)tmScheduler.NextDueTime);   //, "first schedule time nust be schTime1");
      schTime1.ScheduleTime= schTime1.ScheduleTime.AddMilliseconds(200);//reschedule
      Assert.Equal(t, (DateTime)tmScheduler.NextDueTime);   //, "first schedule time nust be schTime1 even when rescheduled");
      Sync.Wait(1, 120);
      Assert.Equal(1, testHandlerCnt1);
      Assert.Equal(0, testHandlerCnt2);
      Assert.Equal(0, testHandlerCnt3);
      Assert.Equal(schTime2.DueDate(startTime), (DateTime)tmScheduler.NextDueTime);   //, "next schedule time nust be schTime2/3");
      Sync.Wait(2, 120);
      Assert.Equal(1, testHandlerCnt1);
      Assert.Equal(1, testHandlerCnt2);
      Assert.Equal(1, testHandlerCnt3);
      Assert.Equal(schTime1.DueDate(startTime), (DateTime)tmScheduler.NextDueTime);   //, "next schedule time nust be schTime1 again");
      Sync.Wait(1, 120);
      Assert.Equal(2, testHandlerCnt1);
      Assert.Equal(1, testHandlerCnt2);
      Assert.Equal(1, testHandlerCnt3);
      Assert.Equal(DateTime.MaxValue, tmScheduler.NextDueTime);   //, "must have no time schedule");

      tmScheduler.Remove(dueTimeTestHandler2);
      tmScheduler.Remove(dueTimeTestHandler3);
      tmScheduler.Remove(dueTimeTestHandler1);
    }
    
    private void DueTimeTestHandler1() {
      ++testHandlerCnt1;
      Sync.Pulse();
    }

    private void DueTimeTestHandler2() {
      ++testHandlerCnt2;
      Sync.Pulse();
    }

    private void DueTimeTestHandler3() {
      ++testHandlerCnt3;
      Sync.Pulse();
    }

   
    class TestScheduleTime : ITimePlan {
      public DateTime ScheduleTime;

      public TestScheduleTime(DateTime time) { this.ScheduleTime= time; }

      public DateTime DueDate(DateTime fromNow) { return ScheduleTime > fromNow ? ScheduleTime : DateTime.MaxValue; }
    }

    class Sync {
      private static Sync sync;
      private int value;
      private Sync() { }
      
      public static void Pulse() {
        var sy= sync;
        if(null != sy) lock (sy) {
          ++sy.value;
          Monitor.Pulse(sy);
        }
      }

      public static void Wait(int cnt, int waitTime) {
        if (null != sync) throw new InvalidOperationException("already waiting...");
        var sy= sync= new Sync();
        var start= App.TimeInfo.Now;
        while (true) lock (sy) {
          if(sy.value >= cnt) break;
          Assert.True(Monitor.Wait(sy, waitTime + 2000));
        }
        sync= null;
        Console.WriteLine("Sync.Wait: {0:G}ms - estimated {1:D}ms", (App.TimeInfo.Now-start).TotalMilliseconds, waitTime);
      }
    }

  }
}
