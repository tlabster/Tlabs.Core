using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tlabs.Sync {

  /// <summary>Thread synchronisation monitor.</summary>
  /// <remarks>
  /// Instances of this class can be used to monitor/signal concurrently applied changes to the state
  /// of an object or value of type T.
  /// <para>While it is based on <see cref="Monitor"/>, it does not suffer the shortcoming of its <see cref=" Monitor.Pulse(object)"/>
  /// method - i.e. this <see cref="SyncMonitor{T}"/> maintains the internal signaled state even if no one
  /// is currently waiting....</para>
  /// <para>For the purpose of synchronization between managed threads, this class would be a lightweight alternative to
  /// <see cref="EventWaitHandle"/> based objects.</para>
  /// <para>Note also that, since the type T is not limited to hold only binary values (like set and reset), there is
  /// no need for <see cref="WaitHandle.WaitAny(WaitHandle[])"/> constructs and alike.</para>
  /// </remarks>
  /// <typeparam name="T">Type of an object or value to monitor the state.</typeparam>
  public class SyncMonitor<T> : BaseMonitor<T> {

    /// <summary>Default ctor.</summary>
    public SyncMonitor() : base(null) { }

    /// <summary>Ctor with specified lock/monitor object.</summary>
    public SyncMonitor(object syncRoot) : base(syncRoot) { }

    /// <summary>Suspends the calling thread until the change of value gets signaled.</summary>
    /// <returns>Current value of type T.</returns>
    public T WaitForSignal() => WaitForSignal(Timeout.Infinite);

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

  }

  /// <summary>Synchronisation monitor whose signaled value could be awaited by a promise <see cref="Task{T}"/> returned from <see cref="AsyncMonitor{T}.SignaledValue(CancellationToken?)"/> .</summary>
  public class AsyncMonitor<T> : SyncMonitor<T> {
    /// <summary>Default ctor.</summary>
    public AsyncMonitor() : base(null) { }

    /// <summary>Ctor with specified lock/monitor object.</summary>
    public AsyncMonitor(object syncRoot) : base(syncRoot) { }

    /// <summary>Source for task result signaling.</summary>
    protected TaskCompletionSource<T> complSrc= new();

    /// <summary>Returns a <see cref="Task{T}"/> to be awaited for a signaled value.</summary>
    public Task<T> SignaledValue(CancellationToken? ctok= null) => SignaledValue(Timeout.Infinite, ctok);

    /// <summary>Returns a <see cref="Task{T}"/> to be awaited for a signaled value.</summary>
    /// <param name="timeOut">Maximum number of milliseconds to wait for a signal until the <see cref="Task{T}"/> is cancelled.</param>
    /// <param name="ctok">Cancellation token</param>
    public Task<T> SignaledValue(int timeOut, CancellationToken? ctok= null) {
      if (IsSignaled) return Task.FromResult(this.Value);
      if (timeOut > 0) {
        var ctokSrc= new CancellationTokenSource(timeOut);
        ctokSrc.Token.Register(() => complSrc.TrySetCanceled());
      }
      ctok?.Register(() => complSrc.TrySetCanceled());
      return complSrc.Task;
    }

    ///<inheritdoc/>
    public override void ResetSignal() {
      base.ResetSignal();
      this.complSrc.TrySetCanceled();
      this.complSrc= new TaskCompletionSource<T>();
    }

    ///<inheritdoc/>
    protected override T IternalSignal(SignalState state= SignalState.OneShot, T val= default(T), bool hasVal= false) {
      val= base.IternalSignal(state, val, hasVal);
      complSrc.TrySetResult(val);
      return val;
    }
  }

  /// <summary>Base synchronisation monitor.</summary>
  public abstract class BaseMonitor<T> {
    /// <summary>Signal states.</summary>
    protected enum SignalState {
      /// <summary>No signal pending.</summary>
      None,
      /// <summary>One single signal pending.</summary>
      OneShot,
      /// <summary>Signal pending permamnent.</summary>
      Permanent
    }
    /// <summary>Sync. root.</summary>
    protected object syncRoot;
    /// <summary>Pending signal state.</summary>
    protected volatile SignalState pendingSignal;
    /// <summary>Value.</summary>
    protected T val;

    /// <summary>Ctor with specified lock/monitor object.</summary>
    protected BaseMonitor(object syncRoot) => this.syncRoot= syncRoot ?? this;

    /// <summary>Getter / setter to the monitored value.</summary>
    /// <remarks>Setting the monitor value through this property does _not_ signal it's change.
    /// (Use <see cref="Signal(T)"/> instead.)</remarks>
    public T Value {
      get { lock (syncRoot) return val; }
      set { lock (syncRoot) val= value; }
    }

    /// <summary>Signal current monitor value to one next waiter.</summary>
    public T Signal() => IternalSignal();

    /// <summary>Set value <paramref name="val"/> and signal it's change to one next waiter.</summary>
    public T Signal(T val) => IternalSignal(SignalState.OneShot, val, true);

    /// <summary>Signal current monitor value to all next waiter.</summary>
    public T SignalPermanent() => IternalSignal(SignalState.Permanent);

    /// <summary>Set value <paramref name="val"/> and starts a permanent signaling of it's change to all waiter(s) until <see cref="ResetSignal()"/>.</summary>
    public T SignalPermanent(T val) => IternalSignal(SignalState.Permanent, val, true);

    /// <summary>Reset signaling of a one-time or permanent signal.</summary>
    public virtual void ResetSignal() {
      lock (syncRoot) pendingSignal= SignalState.None;
    }

    ///<summary>True if signaled.</summary>
    public bool IsSignaled => SignalState.None != pendingSignal;

    /// <summary>Reference to the lock/monitor object used for synchronization.</summary>
    public object Sync => syncRoot;

    /// <summary>Signal monitor <paramref name="state"/> with optional <paramref name="val"/> and <paramref name="hasVal"/> indicator.</summary>
    protected virtual T IternalSignal(SignalState state= SignalState.OneShot, T val= default(T), bool hasVal= false) {
      lock (syncRoot) {
        if (hasVal) this.val= val;
        if (SignalState.Permanent == state) {
          Monitor.PulseAll(syncRoot);
          pendingSignal= state;
          return this.val;
        }
        Monitor.Pulse(syncRoot);
        if (SignalState.None == pendingSignal)
          pendingSignal= state;
        return this.val;
      }
    }

  }

}
