using System;
using System.Threading;

namespace Tlabs.Sync {

  /// <summary>Thread synchronisation monitor.</summary>
  /// <remarks>
  /// Instances of this class can be used to monitor/signal concurrently applied changes to the state
  /// of an object or value of type T.
  /// <para>While it is based on <see cref=" Monitor"/>, it does not suffer the short coming of its <see cref=" Monitor.Pulse(object)"/>
  /// method - i.e. this <see cref="SyncMonitor{T}"/> maintains the internal signaled state even if no one
  /// is currently waiting....</para>
  /// <para>For the purpose of synchronization between managed threads, this class would be an alternative to
  /// <see cref="EventWaitHandle"/> based objects.</para>
  /// <para>Note also that, since the type T is not limited to hold only binary values (like set and reset), there is
  /// no need for <see cref="WaitHandle.WaitAny(WaitHandle[])"/> constructs and alike.</para>
  /// </remarks>
  /// <typeparam name="T">Type of an object or value to monitor the state.</typeparam>
  public class SyncMonitor<T> {
    private enum SignalState { None, OneShot, Permanent }
    private object syncRoot;
    private SignalState pendingSignal;
    private T val;

    /// <summary>Default ctor.</summary>
    public SyncMonitor() : this(null) { }

    /// <summary>Ctor with specified lock/monitor object.</summary>
    public SyncMonitor(object syncRoot) {
      this.syncRoot= syncRoot ?? this;
    }

    /// <summary>Getter / setter to the monitored value.</summary>
    /// <remarks>Setting the monitor value through this property does _not_ signal it's change.
    /// (Use <see cref="SyncMonitor&lt;T&gt;.Signal(T)"/> instead.)</remarks>
    public T Value {
      get { lock (syncRoot) return val; }
      set { lock (syncRoot) val= value; }
    }

    /// <summary>Set value <paramref name="val"/> and signal it's change to one next waiter.</summary>
    public T Signal(T val) {
      lock (syncRoot) {
        this.val= val;
        Monitor.Pulse(syncRoot);
        if (SignalState.None == pendingSignal)
          pendingSignal= SignalState.OneShot;
        return val;
      }
    }

    /// <summary>Signal current monitor value to one next waiter.</summary>
    public T Signal() {
      lock (syncRoot) {
        Monitor.Pulse(syncRoot);
        if (SignalState.None == pendingSignal)
          pendingSignal= SignalState.OneShot;
        return this.val;
      }
    }


#if false //broken_SignalAll
    /* Signalall does not make sense with a SignalState.OneShot signal!!!
     * -> While Monitor.PulseAll() does signal all waiters, the first returning from Monitor.Wait() does reset a OneShot
     *    pendingSignal back into SignalState.None - which in turn keeps other waiters in the wait() loop...
     */
    /// <summary>Set value <paramref name="val"/> and signal it's change to all waiter(s).</summary>
    public T SignalAll(T val) {
      lock (syncRoot) {
        this.val= val;
        Monitor.PulseAll(syncRoot);
        if (SignalState.None == pendingSignal)
          pendingSignal= SignalState.OneShot;
        return this.val;
      }
    }

    /// <summary>Signal current monitor value to all waiter(s).</summary>
    public T SignalAll() {
      lock (syncRoot) {
        Monitor.PulseAll(syncRoot);
        if (SignalState.None == pendingSignal)
          pendingSignal= SignalState.OneShot;
        return this.val;
      }
    }
#endif

    /// <summary>Set value <paramref name="val"/> and starts a permanent signaling of it's change to all waiter(s) until <see cref="ResetSignal()"/>.</summary>
    public T SignalPermanent(T val) {
      lock (syncRoot) {
        this.val= val;
        Monitor.PulseAll(syncRoot);
        pendingSignal= SignalState.Permanent;
        return this.val;
      }
    }

    /// <summary>Signal current monitor value to all next waiter.</summary>
    public T SignalPermanent() {
      lock (syncRoot) {
        Monitor.PulseAll(syncRoot);
        pendingSignal= SignalState.Permanent;
        return this.val;
      }
    }

    /// <summary>Reset signaling of a one-time or permanent signal.</summary>
    public void ResetSignal() {
      lock (syncRoot) pendingSignal= SignalState.None;
    }

    /// <summary>Suspends the calling thread until the change of value gets signaled.</summary>
    /// <returns>Current value of type T.</returns>
    public T WaitForSignal() {
      return WaitForSignal(Timeout.Infinite);
    }

    /// <summary>Suspends the calling thread until the change of value gets signaled.</summary>
    /// <param name="timeOut">Maximum number of milliseconds to wait for a signal.</param>
    /// <returns>Current value of type T.</returns>
    /// <exception cref="TimeoutException">Exception thrown when <paramref name="timeOut"/> milliseconds elapsed
    /// w/o signal.</exception>
    public T WaitForSignal(int timeOut) {
      lock (syncRoot) {
        while (SignalState.None == pendingSignal) {    //while loop, because we might loose the race with another waiter on the lock
          if (!Monitor.Wait(syncRoot, timeOut)) throw new TimeoutException("WaitForSignal() timed out.");
        }
        if (SignalState.OneShot == pendingSignal)
          pendingSignal= SignalState.None;
        return this.val;
      }
    }

    /// <summary>Reference to the lock/monitor object used for synchronization.</summary>
    public object Sync {
      get { return syncRoot; }
    }

  }
}
