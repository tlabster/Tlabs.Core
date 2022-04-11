using System;
using System.Threading;

namespace Tlabs.Sync {

  /// <summary>Key based mutex ticket.</summary>
  ///<remarks>For each givent key given with the ctor <see cref="MuxTicket"/> maintains a internal sync object that is being
  /// acquired before ctor returns....
  ///</remarks>
  public sealed class MuxTicket : IDisposable {
    static readonly SyncTable syncMap= new SyncTable(k => new object());
    /// <summary>Ctor from <paramref name="key"/> and optional <paramref name="timeOut"/>.</summary>
    public MuxTicket(object key, int timeOut = System.Threading.Timeout.Infinite) {
      object sync, acquiredSync= null;
      this.Key= key;

      while (true) {
        /* Retry until sync was acquied and placed in syncMap:
         */
        lock (syncMap) sync= syncMap[Key];
        if (acquiredSync == sync) return;
        if (null != acquiredSync && Monitor.IsEntered(acquiredSync)) Monitor.Exit(acquiredSync); //not from syncMap: release
        if (!Monitor.TryEnter(sync, timeOut)) throw new TimeoutException($"Failed to accquire {nameof(MuxTicket)} within {timeOut} msec.");
        acquiredSync= sync;
      }
    }

    /// <summary>Dtor.</summary>
    ~MuxTicket() => Dispose();

    /// <summary>Ticket key.</summary>
    public object Key { get; }

    /// <summary>Release acquirement (and signal next waiter).</summary>
    public void Dispose() {
      lock (syncMap) {
        var sync= syncMap.Evict(Key);
        if (null == sync || !Monitor.IsEntered(sync)) return;
        Monitor.Exit(sync);
      }
      GC.SuppressFinalize(this);
    }

    private class SyncTable : Misc.LookupTable<object, object> {
      public SyncTable(Func<object, object> create) : base(create) { }
      public object Evict(object key) {
        if (table.TryGetValue(key, out var sync))
          table.Remove(key);
        return sync;
      }
    }

  }

}