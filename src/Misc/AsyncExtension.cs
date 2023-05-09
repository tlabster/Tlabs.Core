using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tlabs.Misc {

  ///<summary>Async (task) extension utils</summary>
  public static class AsyncExtensions {
    ///<summary>Specify a <paramref name="timeout"/> on <paramref name="task"/> with optional <paramref name="ctk"/></summary>
    public static async Task Timeout(this Task task, int timeout, CancellationToken ctk= default) {
      if (task == await Task.WhenAny(task, Task.Delay(timeout, ctk))) {
        return;
      }
      throw new TimeoutException($"{timeout} ms timeout before task completion expired.");
    }

    ///<summary>Specify a <paramref name="timeout"/> on <paramref name="task"/> with optional <paramref name="ctk"/></summary>
    public static async Task<T> Timeout<T>(this Task<T> task, int timeout, CancellationToken ctk= default) {
      if (task == await Task.WhenAny(task, Task.Delay(timeout, ctk)))
        return await task;

      throw new TimeoutException($"{timeout} ms timeout before task completion expired.");
    }

    ///<summary>Convert this <see cref="CancellationToken"/> into a <see cref="Task"/> that could be awaited for cancellation.</summary>
    public static Task AsTask(this CancellationToken cancellationToken) {
      var tcs= new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
      cancellationToken.Register(() => tcs.TrySetCanceled(), false);
      return tcs.Task;
    }

    ///<summary>Await async result with <paramref name="timeout"/> milliseconds.</summary>
    ///<exception cref="TimeoutException">thrown when task was not completed within <paramref name="timeout"/> milliseconds.</exception>
    public static void AwaitWithTimeout(this Task tsk, int timeout, CancellationToken ctk= default)
    => tsk.Timeout(timeout, ctk).GetAwaiter().GetResult();

    ///<summary>Await async result with <paramref name="timeout"/> milliseconds.</summary>
    ///<exception cref="TimeoutException">thrown when task was not completed within <paramref name="timeout"/> milliseconds.</exception>
    public static T AwaitWithTimeout<T>(this Task<T> tsk, int timeout, CancellationToken ctk= default)
    => tsk.Timeout(timeout, ctk).GetAwaiter().GetResult();

  }
}