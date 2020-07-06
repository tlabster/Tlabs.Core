using System;
using System.Collections.Generic;
using System.Linq;

namespace Tlabs.Sync {

  /// <summary>Key based mutex ticket.</summary>
  ///<remarks>For each givent key given with the ctor <see cref="MuxTicket"/> maintains a internal sync object that is being
  /// acquired before ctor returns....
  ///</remarks>
  public sealed class MultiMuxTicket<T> : IDisposable {

    /// <summary>Ctor from <paramref name="keys"/> and optional <paramref name="timeOut"/>.</summary>
    public MultiMuxTicket(IEnumerable<T> keys, int timeOut= System.Threading.Timeout.Infinite) {
      this.Tickets= keys.Select(k => new MuxTicket(k, timeOut)).ToList();
    }

    /// <summary>Dtor.</summary>
    ~MultiMuxTicket() => Dispose();

    /// <summary>Ticket key.</summary>
    public IEnumerable<MuxTicket> Tickets { get; }

    /// <summary>Release acquirement (and signal next waiter).</summary>
    public void Dispose() {
      foreach (var mx in Tickets)
        mx.Dispose();
      GC.SuppressFinalize(this);
    }

  }

}