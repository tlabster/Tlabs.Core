using System;

namespace Tlabs.Misc {

  ///<summary>Execution context to contain a thread-local reference value of <typeparamref name="T"/></summary>
  ///<remarks>This allows to setup some simple context data that will be accessible within the call stack of an <see cref="Action"/>>.</remarks>
  public static class ExecContext<T> where T : class {
    [ThreadStatic]
    private static T? thloc; //a thread local storage of T

    ///<summary>true if has constext data from started context.</summary>
    public static bool HasData => (null != thloc);

    ///<summary>Start a new context with <paramref name="ctxData"/> accessible within the call stack of the <paramref name="ctxAction"/>.</summary>
    ///<exception cref="InvalidOperationException">if context of <typeparamref name="T"/> was already started.</exception>
    public static void StartWith(T ctxData, Action ctxAction) {
      if (null == ctxData) throw new ArgumentException(nameof(ctxData));
      if (null != thloc)
        throw new InvalidOperationException(nameof(ExecContext<T>) + " already started.");

      try {
        thloc= ctxData; // set data on context start
        ctxAction();
      }
      finally {
        thloc= null;    // remove data on context end
      }
    }

    ///<summary>Returns the current context-data of a previously started context.</summary>
    ///<exception cref="InvalidOperationException">if no context was previously started</exception>
    public static T CurrentData {
      get {
        var data= thloc;
        if (null == data) throw new InvalidOperationException(nameof(ExecContext<T>) + " not started.");
        return data;
      }
    }
  }

}