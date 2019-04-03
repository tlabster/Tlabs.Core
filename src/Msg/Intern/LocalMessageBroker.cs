using System;
using System.Collections.Generic;

namespace Tlabs.Msg.Intern {

  ///<inherit/>
  public class LocalMessageBroker : IMessageBroker {
    private Dictionary<string, Delegate> subsciptions= new Dictionary<string, Delegate>();
    ///<inherit/>
    public void Publish(string subject, object msg) => findSubscriptionHandler<object>(subject)?.Invoke(msg);

    ///<inherit/>
    public void Publish<T>(string subject, IMessage<T> msg) where T : class => findSubscriptionHandler<IMessage<object>>(subject)?.Invoke(msg);

    private Action<T> findSubscriptionHandler<T>(string subject) {
      Delegate handler;
      lock(subsciptions) {
        subsciptions.TryGetValue(subject, out handler);
        return handler as Action<T>;
      }
    }

    ///<inherit/>
    public void Subscribe(string subject, Action<object> subHandler) {
      Delegate handler;
      lock (subsciptions) {
        if (subsciptions.TryGetValue(subject, out handler))
          subsciptions[subject]= Delegate.Combine(handler, subHandler);
        else subsciptions[subject]= subHandler;
      }
    }

    ///<inherit/>
    public void Subscribe<T>(string subject, Action<IMessage<T>> subHandler) {
      Delegate handler;
      var subHndl= (Action<IMessage<object>>) subHandler;
      lock (subsciptions) {
        if (subsciptions.TryGetValue(subject, out handler))
          subsciptions[subject]= Delegate.Combine(handler, subHndl);
        else subsciptions[subject]= subHndl;
      }
    }

    ///<inherit/>
    public bool Unsubscribe(Delegate handler) {
      bool fnd= false;
      lock(subsciptions) {
        var keys= new List<string>(subsciptions.Keys);
        foreach(var k in keys) {
          var subHandler= subsciptions[k];
          handler= Delegate.Remove(subHandler, handler);
          if (null == handler) subsciptions.Remove(k);
          else if(object.ReferenceEquals(handler, subHandler)) {
            fnd= true;
            subsciptions[k]= handler;
          }
        }
        return fnd;
      }
    }

  }
}