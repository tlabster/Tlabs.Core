﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tlabs.Misc;

using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Msg.Intern.Tests {

  public class LocalMsgTest {
    LocalMessageBroker msgBroker= new LocalMessageBroker();
    ITestOutputHelper tstout;

    public LocalMsgTest(ITestOutputHelper tstout) {
      this.tstout= tstout;
    }

    [Fact]
    public void BasicTest() {
      int hndlCnt= 0;
      var counter= new Sync.SyncMonitor<int>();

      msgBroker.Publish("test", "Test message");

      Action<object> handler= o => {
        Assert.IsType<string>(o);
        if (Interlocked.Increment(ref hndlCnt) == 2)
         counter.Signal(hndlCnt);
      };
      msgBroker.Subscribe("test", handler);
      msgBroker.Publish("test", "Test message");
      msgBroker.Publish("test", "Another test message");

      msgBroker.Unsubscribe(null);  //do nothing
      msgBroker.Unsubscribe((Action<object>)(o => { }));  //do nothing
      msgBroker.Unsubscribe(handler);
      msgBroker.Publish("test", "Ignored test message");

      Assert.Equal(2, counter.WaitForSignal(50));
    }

    [Fact]
    public void MessageTest() {
      int hndlCnt= 0;
      var counter= new Sync.SyncMonitor<int>();
      var msg= new TestMessage { SourceID= this, Data= new TestPayload { Property= "Test message"} };

      int strMsgCnt= 0;
      int tstMsgCnt= 0;
      Action<object> handler= o => {
        var msgStr= o as string;
        if (string.IsNullOrEmpty(msgStr)) {
          Assert.IsType<TestMessage>(o);
          Assert.IsAssignableFrom<IMessage>(o);
          Interlocked.Increment(ref tstMsgCnt);
        }
        else Interlocked.Increment(ref strMsgCnt);
        if (Interlocked.Increment(ref hndlCnt) == 3)
          counter.Signal(hndlCnt);
      };
      msgBroker.Subscribe("test", handler);
      msgBroker.Publish("test", msg);
      msgBroker.Publish("test", "Test message");
      msgBroker.Publish("test", "Test another message");

      Assert.Equal(3, counter.WaitForSignal(50));
      Assert.Equal(1, tstMsgCnt);
      Assert.Equal(2, strMsgCnt);
    }

    [Fact]
    public void SubscriptionTest() {
      int hndlCnt= 0;
      var counter= new Sync.SyncMonitor<int>();
      var msg= new TestMessage { SourceID= this, Data= new TestPayload { Property= "Test message"} };

      int strMsgCnt= 0;
      int tstMsgCnt= 0;
      Action<IMessage> msgHandler= o => {
        Assert.IsType<TestMessage>(o);
        Interlocked.Increment(ref tstMsgCnt);
        if (Interlocked.Increment(ref hndlCnt) == 4)
          counter.Signal(hndlCnt);
      };
      Action<object> strHandler= o => {
        Interlocked.Increment(ref strMsgCnt);
        if (Interlocked.Increment(ref hndlCnt) == 4)
          counter.Signal(hndlCnt);
      };
      msgBroker.Subscribe("test", msgHandler);
      msgBroker.Subscribe<object>("test", strHandler);
      msgBroker.Publish("test", "Test message");
      msgBroker.Publish("test", "Test another message");
      msgBroker.Publish("test", msg);

      Assert.Equal(4, counter.WaitForSignal(50));
      Assert.Equal(1, tstMsgCnt);
      Assert.Equal(3, strMsgCnt);
    }

    public interface IMessage {
      object SourceID { get; }
    }
    public class TestMessage : IMessage {
      public object SourceID { get; set; }
      public TestPayload Data { get; set; }
    }

    public class TestPayload {
      public string Property { get; set; }
    }


    [Fact]
    public void PublishRequestTest() {
      var msg= new IntMsg { i= 42 };

      msgBroker.SubscribeRequest<IntMsg, string>("req", m => m.ToString());
      var tsk= msgBroker.PublishRequest<string>("req", msg, 50);

      Assert.Equal(msg.ToString(), tsk.Result);
    }

    [Fact]
    public void ParallelPublishRequestTest() {
      msgBroker.SubscribeRequest<IntMsg, string>("request", m => m.ToString());
      const int n= 50;
      var tskMap= new Dictionary<IntMsg, Task<string>>(n);
      var timer= TimingWatch.StartTiming();
      for (var l= 0; l < n; ++l) {
        var msg= new IntMsg { i= l };
        tskMap[msg]= msgBroker.PublishRequest<string>("request", msg, 50);
      }
      //Task.WaitAll(tskMap.Values.ToArray(), 100); //obsolete

      foreach (var pair in tskMap)
        Assert.Equal(pair.Key.ToString(), pair.Value.Result);

      tstout.WriteLine($"{timer.GetElapsedMilliseconds()}ms for {n} PublisRequest()s");
    }

    public class IntMsg {
      public int i;
      public override string ToString() => $"Return {this.i}";
      public override int GetHashCode() => i;
      public override bool Equals(object obj) {
        var msg= obj as IntMsg;
        if (null == msg) return false;
        return i == msg.i;
      }
    }
  }
}