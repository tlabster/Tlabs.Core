using System;
using System.Threading;

namespace Tlabs.Sync {

  /// <summary>Mutual exclusive accessible object.</summary>
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
        if (!Monitor.IsEntered(syncRoot)) throw new InvalidOperationException("Mutex not acquired.");
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
