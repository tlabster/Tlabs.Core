﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Tlabs.Timing {

  /// <summary>Interface to access the next due-date of a scheduled time.</summary>
  public interface ITimePlan {
  
    /// <summary>Returns the next due-date <paramref name="fromNow"/>.</summary>
    /// <remarks>NOTE: Implementation must return due-dates which are in the future from <paramref name="fromNow"/>.
    /// <para>i.e. when a <see cref="ITimePlan"/> implementation returnde a due-date,
    /// the next call to the <c>DueDate()</c> method with a new <c>fromNow</c> argument which is the last due-date
    /// plus a small milli.sec. amount (<see cref="TimeScheduler.RESOLUTION_MSEC"/>)
    /// MUST return a new due-date &gt; fromNow!!!
    /// </para>
    /// </remarks>
    /// <returns>
    /// Next due-date <paramref name="fromNow"/> or <see cref="DateTime.MaxValue"/>
    /// if schedule is expired (will never become due).
    /// </returns>
    DateTime DueDate(DateTime fromNow);
  }

  /// <summary>Provides a service for executing <see cref="Action"/> (delegates) at specified time schedules.</summary>
  /// <remarks>
  /// Instances of this class maintain an internal list of <see cref="ITimePlan"/> objects
  /// and their associated <see cref="Action"/> delegates, that have been added with the method <see cref="Add(ITimePlan, Action)"/>.<br/>
  /// It arranges to invoke each <see cref="Action"/> delegate once its due-time has arrived
  /// and then re-schedules its <see cref="ITimePlan"/> to invoke it again should it becomes due again.
  /// <para>
  /// NOTE: Any logic which determines when a certain action becomes due, has to be implemented by the <see cref="ITimePlan"/>
  /// objects. This also includes the rules to return the next due date from a point of time. (If this due date would be in the past,
  /// the action won't be called again...)
  /// </para>
  /// </remarks>
  public sealed class TimeScheduler : IDisposable {
    /// <summary>Resolution in milli. sec.</summary>
    public const double RESOLUTION_MSEC= Math.PI;
    /// <summary>custom format to display <c>DateTime</c> values</summary>
    public const string TIME_FORMAT= "yyyy'-'MM'-'dd HH':'mm':'ss";

    internal static readonly ILogger Log= App.Logger<TimeScheduler>();

    private Timer timer;    //***TODO: replace Timer with Task.Delay(waitMili).ContinnueWith(HandleDueTime);
    private LinkedList<ScheduleInfo> schedule= new LinkedList<ScheduleInfo>();

    /// <summary>Default ctor</summary>
    public TimeScheduler() { }

    /// <summary>Next due-time scheduled (could be <see cref="DateTime.MaxValue"/>).</summary>
    public DateTime NextDueTime {
      get {
        lock (schedule) {
          if (0 == schedule.Count) {
            if (null != timer) throw new InvalidOperationException("Internal timer must be null");
            return DateTime.MaxValue;
          }
          return schedule.First.Value.DueDate;
        }
      }
    }

    /// <summary>Add a new time schedule.</summary>
    public void Add(ITimePlan time, Action dueTimeCallee) {
      lock (schedule) {
        Add(new ScheduleInfo(time, dueTimeCallee), App.TimeInfo.Now);
        UpdateTimer();
      }
    }

    /// <summary>Remove all time schedule(s) for <paramref name="dueTimeCallee"/>.</summary>
    public void Remove(Action dueTimeCallee) {
      lock (schedule) {
        LinkedListNode<ScheduleInfo> nd, next;
        for (nd= schedule.First; null != nd; nd= next) {
          next= nd.Next;
          if (dueTimeCallee == nd.Value.Callee)
            schedule.Remove(nd);
        }
        if (0 == schedule.Count)
          Dispose();
      }
    }

    /// <summary>Dispose any unmanaged resources.</summary>
    public void Dispose() {
      if (null == this.timer) return;   //already disposed
      timer.Dispose();
      timer= null;
    }

    private void Add(ScheduleInfo schInfo, DateTime fromNow) {
      if (null == this.timer) this.timer= new Timer(HandleDueTime, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

      schInfo.Update(fromNow);
      for (var nd= schedule.First; null != nd; nd= nd.Next) {
        if (schInfo.DueDate < nd.Value.DueDate) {
          schedule.AddBefore(nd, schInfo);
          if (Log.IsEnabled(LogLevel.Debug)) Log.LogDebug($"New time schedule, due at: {string.Format("{0:" + TimeScheduler.TIME_FORMAT + "}", schInfo.DueDate)}");
          return;
        }
      }
      schedule.AddLast(schInfo);
    }

    private void HandleDueTime(object o) {
      lock (schedule) {
        Log.LogDebug("Processing due time(s)");

        var fromNow= App.TimeInfo.Now.AddMilliseconds(RESOLUTION_MSEC);
        LinkedListNode<ScheduleInfo> schNode;

        while (null != (schNode= schedule.First) && schNode.Value.DueDate <= fromNow) {
          /* Invoke all due-time callees which have become due and re-schedule:
           */
          var schInfo= schNode.Value;

          if (Log.IsEnabled(LogLevel.Debug))  Log.LogDebug($"Invoking callee of {schInfo.Callee.Target} with due-time: {string.Format("{0:" + TimeScheduler.TIME_FORMAT + "}", schInfo.DueDate)}");
          Task.Run(schInfo.Callee);   //schInfo.Callee.BeginInvoke(null, null); //invoke asynch.
          schedule.RemoveFirst();
          Add(schInfo, fromNow);
        }
        UpdateTimer();
      }
    }

    private void UpdateTimer() {
      /* Set timer to wait for next due-time:
       */
      int nextDueTime= Timeout.Infinite;
      LinkedListNode<ScheduleInfo> firstNode;
      if (null != (firstNode= schedule.First) && DateTime.MaxValue != firstNode.Value.DueDate) {
        nextDueTime= (int)(firstNode.Value.DueDate - App.TimeInfo.Now).TotalMilliseconds;
        if (nextDueTime < 0) nextDueTime= 0;  //start immediately if overdue
        else if (nextDueTime > Int32.MaxValue) nextDueTime= Int32.MaxValue;
      }
      if (null != timer)
        timer.Change(nextDueTime, Timeout.Infinite);
      Log.LogDebug("Set timer wake-up in {T}ms", nextDueTime);
    }

    class ScheduleInfo {
      public DateTime DueDate;
      public ITimePlan ScheduleTime;
      public Action Callee;

      public ScheduleInfo(ITimePlan time, Action callee) {
        if (null == (this.ScheduleTime= time)) throw new ArgumentNullException("shedule time");
        if (null == (this.Callee= callee)) throw new ArgumentNullException("callee");
        this.DueDate= DateTime.MaxValue;  //default before first update
      }

      public void Update(DateTime fromNow) {
        DueDate= ScheduleTime.DueDate(fromNow);
      }
    }

  }//class TimeScheduler
}
