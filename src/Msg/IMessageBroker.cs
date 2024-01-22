using System;
using System.Threading.Tasks;

namespace Tlabs.Msg {

  ///<summary>Message broker interface.</summary>
  ///<remarks>
  ///This is the generic interface of a message broker.
  ///<para>While a basic implementation as a local service is <sse cref="Intern.LocalMessageBroker"/>, this is inteneded to be implemented based on on a stand-alone
  ///message broker implementation client like: https://github.com/nats-io/csharp-nats .</para>
  ///<para>Notes on:</para>
  ///<list>
  ///<item><term>Subject</term>
  ///<description>
  ///The format of the subscription-subjects is strongly message broker specific.
  ///Specific implementations are free to parse the subject string into sub-parts like: subject-path, group-queue, reply-subbject etc...
  ///</description></item>
  ///
  ///<item><term>Publish</term>
  ///<description>
  ///Publishing on a subject does not guarantee that there is actually at least one subscriber to handle the message. (Any published message w/o subscriber
  ///is just ignored.)
  ///</description></item>
  ///
  ///<item><term>Subscribe</term>
  ///<description>
  ///When subscribed on a specific message type <see cref="IMessageBroker.Subscribe{T}(string, Action{T})"/> the handler gets invoked only for messages of that type (or derived).
  ///</description></item>
  ///</list>
  ///</remarks>
  public interface IMessageBroker {

    ///<summary>Publish a <paramref name="message"/> on <paramref name="subject"/>.</summary>
    void Publish(string subject, object message);

    ///<summary>Publish a request <paramref name="message"/> on <paramref name="subject"/> to asynchronously return <typeparamref name="TRet"/> with optional <paramref name="timeout"/>).</summary>
    Task<TRet> PublishRequest<TRet>(string subject, object message, int timeout= 0) where TRet : class;

    ///<summary>Subscribe on <paramref name="subject"/> to receive messages of type type <typeparamref name="T"/>.</summary>
    void Subscribe<T>(string subject, Action<T> subHandler) where T : notnull;

    ///<summary>Subscribe on <paramref name="subject"/> to receive messages of type type <typeparamref name="T"/>.</summary>
    void Subscribe<T>(string subject, Func<T, Task> subHandler) where T : notnull;

    ///<summary>Subscribe for a request on <paramref name="subject"/> to receive messages of type type <typeparamref name="TMsg"/> and to return <typeparamref name="TRet"/>.</summary>
    void SubscribeRequest<TMsg, TRet>(string subject, Func<TMsg, TRet> requestHandler) where TMsg : notnull where TRet : notnull;

    ///<summary>Subscribe for a request on <paramref name="subject"/> to receive async. messages of type type <typeparamref name="TMsg"/> and to return <typeparamref name="TRet"/>.</summary>
    void SubscribeRequest<TMsg, TRet>(string subject, Func<TMsg, Task<TRet>> requestHandler) where TMsg : notnull where TRet : notnull;

    ///<summary>Unsubscribe <paramref name="handler"/> from any subscriptions.</summary>
    ///<returns>true if successfull unsubscribed.</returns>
    void Unsubscribe(Delegate? handler);
  }

}