using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Tlabs.Config;

namespace Tlabs.Msg.Intern {

  ///<inherit/>
  public class LocalMessageBroker : IMessageBroker {
    /* NOTE: Currently we do not support subsciptions on wild-card subjects...
     */

    const string RESPONSE_PFX= "_>$response.";
    private Dictionary<string, Func<object, Task>> msgHandlers= new Dictionary<string, Func<object, Task>>();     //msgHandler by subject
    private Dictionary<Delegate, SubscriptionInfo> subscriptions= new Dictionary<Delegate, SubscriptionInfo>();   //subscriptionInfo by (original) subHandler

    ///<inherit/>
    public void Publish(string subject, object msg) => findMessageHandler(subject)?.Invoke(msg);

    private Func<object, Task> findMessageHandler(string subject) {
      Func<object, Task> msgHandler;
      lock(msgHandlers) {
        msgHandlers.TryGetValue(subject, out msgHandler);
        return msgHandler;
      }
    }

    ///<inherit/>
    public Task<TRes> PublishRequest<TRes>(string subject, object message) where TRes : class {
      var compl= new TaskCompletionSource<TRes>();
      Action<TRes> completer= null;
      completer= response => {
        Unsubscribe(completer);
        compl.SetResult(response);
      };
      Subscribe<TRes>(RESPONSE_PFX + subject, completer);   //subscribe for request response
      Publish(subject, message);  // publish the request message
      return compl.Task;
    }

    ///<inherit/>
    public void Subscribe<T>(string subject, Action<T> subHandler) where T : class {
      Func<object, Task> msgHandler;
      lock (msgHandlers) {
        var proxy= createAsyncProxy<T>(subHandler);
        subscriptions[subHandler]= new SubscriptionInfo(subject, proxy);
        if (msgHandlers.TryGetValue(subject, out msgHandler))
          msgHandlers[subject]= (Func<object, Task>)Delegate.Combine(msgHandler, proxy);
        else msgHandlers[subject]= proxy;
      }
    }

    ///<inherit/>
    public void SubscribeRequest<TMsg, TRet>(string subject, Func<TMsg, TRet> requestHandler) where TMsg : class {
      Func<object, Task> msgHandler;
      lock (msgHandlers) {
        var proxy= createAsyncReqProxy<TMsg, TRet>(requestHandler, RESPONSE_PFX + subject);
        subscriptions[requestHandler]= new SubscriptionInfo(subject, proxy);
        if (msgHandlers.TryGetValue(subject, out msgHandler))
          msgHandlers[subject]= (Func<object, Task>)Delegate.Combine(msgHandler, proxy);
        else msgHandlers[subject]= proxy;
      }
    }

    ///<inherit/>
    public void Unsubscribe(Delegate handler) {
      if (null == handler) return;
      lock(msgHandlers) {
        SubscriptionInfo subscription;
        if (! subscriptions.TryGetValue(handler, out subscription)) return; //unknown handler
        subscriptions.Remove(handler);
        Func<object, Task> handlerDel;
        if (! msgHandlers.TryGetValue(subscription.Subject, out handlerDel)) return;
        if (null == Delegate.Remove(handlerDel, subscription.MsgHandler)) msgHandlers.Remove(subscription.Subject);   //no subscription left on this subject
      }
    }

    private Func<object, Task> createAsyncProxy<T>(Delegate subHandler) where T : class {
      return async (o) => {
        var msg= o as T;
        if (null != msg) {
          await Task.Yield();
          ((Action<T>)subHandler).Invoke(msg);
        }
      };
    }
    private Func<object, Task> createAsyncReqProxy<TMsg, TRet>(Delegate subHandler, string response) where TMsg : class {
      return async (o) => {
        var msg= o as TMsg;
        if (null != msg) {
          await Task.Yield();
          Publish(response, ((Func<TMsg, TRet>)subHandler).Invoke(msg));
        }
      };
    }

    private struct SubscriptionInfo {
      public string Subject;
      public Delegate MsgHandler;

      public SubscriptionInfo(string subject, Delegate handler) {
        this.Subject= subject;
        this.MsgHandler= handler;
      }
    }
    ///<summary>Service configurator.</summary>
    public class Configurator : IConfigurator<IServiceCollection> {
      ///<inherit/>
      public void AddTo(IServiceCollection svcColl, IConfiguration cfg) {
        svcColl.AddSingleton<IMessageBroker, LocalMessageBroker>();
      }
    }

  }
}