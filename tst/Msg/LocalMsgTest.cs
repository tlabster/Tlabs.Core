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
      msgBroker.Unsubscribe(handler);
      msgBroker.Publish("test", "Ignored test message");
      Assert.Equal(hndlCnt, 2);
    }
  }

}