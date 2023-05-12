using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Tlabs.Misc.Tests {

  public class AsyncExtensionTest {

    [Fact]
    public async void TimeoutTest() {
      var tcs= new TaskCompletionSource();
      using var cts= new CancellationTokenSource();

      await Assert.ThrowsAsync<TimeoutException>(() => tcs.Task.Timeout(1));

      var cancelledTsk= Assert.ThrowsAsync<TaskCanceledException>(() => tcs.Task.Timeout(10, cts.Token));
      cts.Cancel();
      await cancelledTsk;

      var tsk= tcs.Task.Timeout(-1);
      tcs.SetResult();
      await tsk;
      Assert.True(tsk.IsCompleted);
    }

    [Fact]
    public async void TimeoutResultTest() {
      var tcs= new TaskCompletionSource<int>();
      using var cts= new CancellationTokenSource();

      await Assert.ThrowsAsync<TimeoutException>(() => tcs.Task.Timeout(1));

      var cancelledTsk= Assert.ThrowsAsync<TaskCanceledException>(() => tcs.Task.Timeout(10, cts.Token));
      cts.Cancel();
      await cancelledTsk;

      var tsk= tcs.Task.Timeout(-1);
      tcs.SetResult(3);
      Assert.Equal(3, await tsk);
    }

    [Fact]
    public void AwaitWithTimeoutTest() {
      var tcs= new TaskCompletionSource();

      Assert.Throws<TimeoutException>(() => tcs.Task.AwaitWithTimeout(1));

      using var cts= new CancellationTokenSource(3);
      Assert.Throws<TaskCanceledException>(() => tcs.Task.AwaitWithTimeout(-1, cts.Token));

      Assert.Throws<InvalidOperationException>(() => Task.Run(() => throw new InvalidOperationException()).AwaitWithTimeout(3));

      tcs.SetResult();
      tcs.Task.AwaitWithTimeout(1);
    }

    [Fact]
    public void AwaitResultWithTimeoutTest() {
      var tcs= new TaskCompletionSource<int>();

      Assert.Throws<TimeoutException>(() => tcs.Task.AwaitWithTimeout(1));

      using var cts= new CancellationTokenSource(3);
      Assert.Throws<TaskCanceledException>(() => tcs.Task.AwaitWithTimeout(-1, cts.Token));

      Assert.Throws<InvalidOperationException>(() => Task.Run(() => throw new InvalidOperationException()).AwaitWithTimeout(3));

      tcs.SetResult(3);
      Assert.Equal(3, tcs.Task.AwaitWithTimeout(1));
    }
  }
}