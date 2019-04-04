﻿using System;
using System.Collections.Generic;

namespace Tlabs.Msg.Intern {

  ///<inherit/>
  public class LocalMessageBroker : IMessageBroker {
    /* NOTE: Currently we do not support subsciptions on wild-card subjects...
     */
    private Dictionary<string, SubscriptionHandler> subsciptions= new Dictionary<string, SubscriptionHandler>();
    ///<inherit/>
    public void Publish(string subject, object msg) => findSubscriptionHandler(subject)?.Invoke(msg);

    ///<inherit/>
    public void Publish<T>(string subject, T msg) where T : class => findSubscriptionHandler(subject)?.Invoke(msg);

    private Action<object> findSubscriptionHandler(string subject) {
      SubscriptionHandler handler;
      lock(subsciptions) {
        subsciptions.TryGetValue(subject, out handler);
        return handler?.MsgDelegate;
      }
    }

    ///<inherit/>
    public void Subscribe(string subject, Action<object> subHandler) {
      SubscriptionHandler handler;
      lock (subsciptions) {
        if (subsciptions.TryGetValue(subject, out handler))
          handler.Add<object>(subHandler);
        else subsciptions[subject]= SubscriptionHandler.Create<object>(subHandler);
      }
    }

    ///<inherit/>
    public void Subscribe<T>(string subject, Action<T> subHandler) where T : class {
      SubscriptionHandler handler;
      lock (subsciptions) {
        if (subsciptions.TryGetValue(subject, out handler))
          handler.Add<T>(subHandler);
        else subsciptions[subject]= SubscriptionHandler.Create<T>(subHandler);
      }
    }

    ///<inherit/>
    public void Unsubscribe(Delegate handler) {
      lock(subsciptions) {
        var keys= new List<string>(subsciptions.Keys);
        foreach(var k in keys) {
          var handlerDel= subsciptions[k];
          if (null == handlerDel.Remove(handler)) subsciptions.Remove(k);
        }
      }
    }

    /* Internal class to manage any subscription handler(s) of a subject with a MulticastDelegate.
     * Handlers that expect a specific message of type <T> get proxied in a 'checkedHandler' that
     * only delegates to the subscription-handler if the message type is matching.
     * For the purpose of unsubscribing the original subscription-handler is kept as key into orgSubHandlers...
     */
    private class SubscriptionHandler {
      public static SubscriptionHandler Create<T>(Delegate subHandler) where T : class {
        var hdel= new SubscriptionHandler(subHandler);
        hdel.Add<T>(subHandler);
        return hdel;
      }
      private Dictionary<Delegate, Delegate> orgSubHandlers= new Dictionary<Delegate, Delegate>();
      private SubscriptionHandler(Delegate subHandler) {
        this.MsgDelegate= null;
        this.SubscrDelegate= subHandler;
      }
      public Delegate SubscrDelegate { get; }
      public Action<object> MsgDelegate { get; private set; }
      public void Add<T>(Delegate subHandler) where T: class {
        Action<object> checkedDel= (o) => {
          var msg= o as T;
          if (null != msg)
            ((Action<T>)SubscrDelegate).Invoke(msg);
        };
        var msgDel=   typeof(T).Equals(typeof(object))
                    ? subHandler
                    : orgSubHandlers[subHandler]= checkedDel;
        MsgDelegate= (Action<object>)(  null == MsgDelegate
                                      ? msgDel
                                      : Delegate.Combine(MsgDelegate, msgDel));
      }

      public Delegate Remove(Delegate subHandler) {
        if (null == subHandler) return MsgDelegate;
        var msgHandler= subHandler;
        if (orgSubHandlers.TryGetValue(subHandler, out msgHandler))
          orgSubHandlers.Remove(subHandler);
        return MsgDelegate= (Action<object>)Delegate.Remove(MsgDelegate, msgHandler ?? subHandler as Action<object>);
      }
    }
  }
}