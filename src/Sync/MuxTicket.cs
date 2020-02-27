using System;
using System.Threading;

namespace Tlabs.Sync {

  /// <summary>Key of <typeparamref name="T"/> based mutex ticket.</summary>
  public sealed class MuxTicket<T> : IDisposable {
    static readonly SyncTable<T> syncMap= new SyncTable<T>(k => new object());
    /// <summary>Ctor from <paramref name="key"/> and optional <paramref name="timeOut"/>.</summary>
    public MuxTicket(T key, int timeOut = System.Threading.Timeout.Infinite) {
      object sync, acquiredSync= null;
      
      while (true) {
        /* Retry until sync was acquied and placed in syncMap:
         */
        lock (syncMap) sync= syncMap[Key];
        if (acquiredSync == sync) return;
        if (null != acquiredSync && Monitor.IsEntered(acquiredSync)) Monitor.Exit(acquiredSync); //not from syncMap: release
        if (!Monitor.TryEnter(sync, timeOut)) throw new TimeoutException($"Failed to accquire {nameof(MuxTicket<T>)} within {timeOut} msec.");
        acquiredSync= sync;
      }
    }

    /// <summary>Dtor.</summary>
    ~MuxTicket() => Dispose();

    /// <summary>Ticket key.</summary>
    public T Key { get; set; }

    /// <summary>Release acquirement (and signal next waiter).</summary>
    public void Dispose() {
      lock (syncMap) {
        var sync= syncMap.Evict(Key);
        if (null == sync || !Monitor.IsEntered(sync)) return;
        Monitor.Exit(sync);
      }
      GC.SuppressFinalize(this);
    }

    private class SyncTable<K> : Misc.LookupTable<K, object> {
      public SyncTable(Func<K, object> create) : base(create) { }
      public object Evict(K key) {
        object sync;
        if (table.TryGetValue(key, out sync))
          table.Remove(key);
        return sync;
      }
    }

  }

}