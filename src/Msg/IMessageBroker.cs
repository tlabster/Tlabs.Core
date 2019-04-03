using System;


namespace Tlabs.Msg {

  ///<summary>Message broker interface.</summary>
  ///<remarks>
  ///This is the generic interface of a message broker.
  ///<para>While a basic implementation as a local service is <sse cref="Intern.LocalMessageBroker"/>, this is inteneded to be implemented based on on a stand-alone
  ///message broker implementation client like: https://github.com/nats-io/csharp-nats .</para>
  ///<para>Notes on:</para>
  ///<list>
  ///<term>Subject</term>
  ///<description>
  ///The format of the subscription-subjects is strongly message broker specific.
  ///Specific implementation are free to parse the subject string into sub-parts like: subject-path, group-queue, reply-subbject etc...
  ///</description>
  ///
  ///<term>Publish</term>
  ///<description>
  ///Publishing on a subject does not guarantee that there is actually at least one subscriber to handle the message. (Any published message w/o subscriber
  ///is just ignored.)
  ///</description>
  ///</list>
  ///</remarks>
  public interface IMessageBroker {

    ///<summary>Publish a <paramref name="message"/> on <paramref name="subject"/> with unspecific format.</summary>
    void Publish(string subject, object message);

    ///<summary>Publish a <paramref name="message"/> of <typeparamref name="T"/> on <paramref name="subject"/>.</summary>
    void Publish<T>(string subject, IMessage<T> message) where T : class;

    ///<summary>Subscribe on <paramref name="subject"/> to receive messages with unspecific format.</summary>
    void Subscribe(string subject, Action<object> subHandler);

    ///<summary>Subscribe on <paramref name="subject"/> to receive messages of type <typeparamref name="T"/>.</summary>
    void Subscribe<T>(string subject, Action<IMessage<T>> subHandler);

    ///<summary>Unsubscribe <paramref name="handler"/> from any subscriptions.</summary>
    ///<returns>true if successfull unsubscribed.</returns>
    bool Unsubscribe(Delegate handler);
  }


  public interface IMessage<out T> {
    object SourceID { get; }
    T Data { get; }
  }
}