using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tlabs.Diagnostic {

  /// <summary>Diagnostic extensions.</summary>
  public static class DiagnosticExt {
    /// <summary>Utility method to subscribe to a <see cref="DiagnosticListener"/>.</summary>
    public static IDisposable SubscribeToListener(string listenerName, string eventName, Action<object> handler) {
      IDisposable subscription= null;
      var observer= new System.Reactive.AnonymousObserver<KeyValuePair<string, object>>(pair => { if (pair.Key == eventName) handler(pair.Value); });
      Predicate<string> predicate= e => eventName == e;

      var discover= DiagnosticListener.AllListeners.Subscribe(listener => {
        if (listenerName == listener.Name)
          subscription= listener.Subscribe(observer, predicate);
      });

      return subscription;
    }

    /// <summary>Utility method to subscribe to a <see cref="DiagnosticListener"/>.</summary>
    public static IDisposable SubscribeToListener(string listenerName, string eventName, Func<object, object, bool> isEnabled, Action<object> handler) {
      IDisposable subscription= null;
      var observer= new System.Reactive.AnonymousObserver<KeyValuePair<string, object>>(pair => { if (pair.Key == eventName) handler(pair.Value); });
      Func<string, object, object, bool> predicate= (e, c1, c2) => eventName == e && null != isEnabled && isEnabled(c1, c2);
      var discover= DiagnosticListener.AllListeners.Subscribe(listener => {
        if (listenerName == listener.Name)
          subscription= listener.Subscribe(observer, predicate);
      });

      return subscription;
    }

    /// <summary>Add (config) properties to the activitis tag enumeration.</summary>
    public static void AddPropTags(this Activity act, IReadOnlyDictionary<string, object> props) {
      foreach (var pair in props)
        act.AddTag(pair.Key, pair.Value?.ToString());
    }

    /// <summary>Log event with <paramref name="src"/>.</summary>
    public static T LogEvent<T>(this DiagnosticSource src, string evName, T obj) {
      if (src.IsEnabled(evName))
        src.Write(evName, obj);
      return obj;
    }
  }

}