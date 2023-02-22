using System;
using System.Threading;

namespace Tlabs.Sync {

  /// <summary>Mutual exclusive accessible object.</summary>
  ///<remarks>
  ///<para>CAUTION:</para>
  ///This is problematic whem using in an async context:
  ///<code>
  ///async Task method() {
  ///  using (var mtx= new Mutex&lt;int>()) {
  ///    ...
  ///    await doAsync();     // lt;-- this could return with different thread!!! Thus not rleasing the mutex with Dispose() and causing a deadlock...
  ///  }
  ///}
  ///</code>
  ///</remarks>
  [Obsolete("MUST not be used with await !", false)]
  public sealed class Mutex<T> : IDisposable {
    private readonly object syncRoot;
    private T value;


    /// <summary>Ctor to wrap <paramref name="value"/> into the mutex with <paramref name="timeOut"/> and <paramref name="syncRoot"/>.</summary>
    public Mutex(T value, int timeOut, object syncRoot) {
      this.syncRoot= syncRoot;
      if (!Monitor.TryEnter(syncRoot, timeOut)) throw new TimeoutException($"Failed to accquire mutex within {timeOut} msec.");
      this.value= value;
    }

    /// <summary>Ctor to wrap <paramref name="value"/> into the mutex.></summary>
    public Mutex(T value, int timeOut= System.Threading.Timeout.Infinite) : this(value, timeOut, value) { }

    /// <summary>Dtor.</summary>
    ~Mutex() => Dispose();

    /// <summary>Wrapped  mutex value.</summary>
    public T Value {
      get {
        if (!Monitor.IsEntered(syncRoot)) throw new SynchronizationLockException("Mutex not acquired.");
        return value;
      }
    }

    /// <summary>Release acquirement (and signal next waiter).</summary>
    public void Dispose() {
      if (!Monitor.IsEntered(syncRoot)) return;
      Monitor.Exit(syncRoot);
      value= default(T);
      GC.SuppressFinalize(this);
    }
  }
}
