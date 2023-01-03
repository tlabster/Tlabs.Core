using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tlabs.Sync {

  ///<summary>Async (task) extension utils</summary>
  public static class AsyncExtensions {
    ///<summary>Specify a <paramref name="timeout"/> on <paramref name="task"/> with optional <paramref name="cts"/></summary>
    public static async Task<T> Timeout<T>(this Task<T> task, TimeSpan timeout, CancellationTokenSource cts= null) {
      if (task == await Task.WhenAny(task, Task.Delay(timeout)))
        return await task;

      cts?.Cancel();
      throw new TimeoutException($"{timeout} ms timeout before task completion expired.");
    }
  }
}