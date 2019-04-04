using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Msg.Intern.Tests {

  public class LocalMsgTest {
    LocalMessageBroker msgBroker= new LocalMessageBroker();

    [Fact]
    public void BasicTest() {

      msgBroker.Publish("test", "Test message");

      int hndlCnt= 0;
      Action<object> handler= o => {
        Assert.IsType<string>(o);
        ++hndlCnt;
      };
      msgBroker.Subscribe("test", handler);
      msgBroker.Publish("test", "Test message");
      msgBroker.Publish("test", "Another test message");

      msgBroker.Unsubscribe(null);  //do nothing
      msgBroker.Unsubscribe((Action<object>)(o => { }));  //do nothing
      msgBroker.Unsubscribe(handler);
      msgBroker.Publish("test", "Ignored test message");

      Assert.Equal(2, hndlCnt);
    }

    [Fact]
    public void MessageTest() {
      var msg= new TestMessage { SourceID= this, Data= new TestPayload { Property= "Test message"} };

      int hndlCnt= 0;
      int strMsgCnt= 0;
      int tstMsgCnt= 0;
      Action<object> handler= o => {
        var msgStr= o as string;
        if (string.IsNullOrEmpty(msgStr)) {
          Assert.IsType<TestMessage>(o);
          Assert.IsAssignableFrom<IMessage>(o);
          ++tstMsgCnt;
        } else ++strMsgCnt;
        ++hndlCnt;
      };
      msgBroker.Subscribe("test", handler);
      msgBroker.Publish("test", msg);
      msgBroker.Publish("test", "Test message");
      msgBroker.Publish("test", "Test another message");

      Assert.Equal(3, hndlCnt);
      Assert.Equal(1, tstMsgCnt);
      Assert.Equal(2, strMsgCnt);
    }

    [Fact]
    public void SubscriptionTest() {
      var msg= new TestMessage { SourceID= this, Data= new TestPayload { Property= "Test message"} };

      int strMsgCnt= 0;
      int tstMsgCnt= 0;
      Action<IMessage> msgHandler= o => {
        Assert.IsType<TestMessage>(o);
        ++tstMsgCnt;
      };
      Action<object> strHandler= o => {
        ++strMsgCnt;
      };
      msgBroker.Subscribe("test", msgHandler);
      msgBroker.Subscribe("test", strHandler);
      msgBroker.Publish("test", msg);
      msgBroker.Publish("test", "Test message");
      msgBroker.Publish("test", "Test another message");

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
  }
}