using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tlabs.Misc {

  ///<summary>Async (task) extension utils</summary>
  public static class AsyncExtensions {
    ///<summary>Specify a <paramref name="timeout"/> on <paramref name="task"/> with optional <paramref name="ctk"/></summary>
    public static async Task Timeout(this Task task, int timeout, CancellationToken ctk= default) {
      var cts= CancellationTokenSource.CreateLinkedTokenSource(ctk);
      try {
        var anyTsk= await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
        await anyTsk;   //throw any exception
        if (task == anyTsk) return;

        throw new TimeoutException($"{timeout} ms timeout before task completion expired.");
      }
      finally {
        cts.Cancel();
        cts.Dispose();
      }
    }

    ///<summary>Specify a <paramref name="timeout"/> on <paramref name="task"/> with optional <paramref name="ctk"/></summary>
    public static async Task<T> Timeout<T>(this Task<T> task, int timeout, CancellationToken ctk= default) {
      var cts= CancellationTokenSource.CreateLinkedTokenSource(ctk);
      try {
        var anyTsk= await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
        if (task == anyTsk) return await task;

        await anyTsk;   //throw any exception
        throw new TimeoutException($"{timeout} ms timeout before task completion expired.");
      }
      finally {
        cts.Cancel();
        cts.Dispose();
      }
    }

    ///<summary>Convert this <see cref="CancellationToken"/> into a <see cref="Task"/> that could be awaited for cancellation.</summary>
    public static Task AsTask(this CancellationToken cancellationToken, bool completeOnCancel= false) {
      var tcs= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      cancellationToken.Register(() => {
        if (completeOnCancel) tcs.TrySetResult();
        else tcs.TrySetCanceled();
      }, false);
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