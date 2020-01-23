using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tlabs.Misc {

  ///<summary>Task await with timeout extension methods.</summary>
  public static class TaskAwaiterExtension {

    ///<summary>Await async result with <paramref name="timeout"/> milliseconds.</summary>
    ///<exception cref="TimeoutException">thrown when task was not completed within <paramref name="timeout"/> milliseconds.</exception>
    public static T AwaitResult<T>(this Task<T> tsk, int timeout)
    => AwaitResultAsync(tsk, timeout).GetAwaiter().GetResult();

    ///<summary>Await async result with <paramref name="timeout"/> milliseconds.</summary>
    ///<returns>Completed task.</returns>
    ///<exception cref="TimeoutException">thrown when task was not completed within <paramref name="timeout"/> milliseconds.</exception>
    public static async Task<T> AwaitResultAsync<T>(this Task<T> tsk, int timeout) {
      using (var ctokSrc = new CancellationTokenSource()) {
        if (tsk != await Task.WhenAny(tsk, Task.Delay(timeout, ctokSrc.Token)))
          throw new TimeoutException();

        ctokSrc.Cancel();
      }
      return await tsk;  // Very important in order to propagate exceptions
    }

  }


}