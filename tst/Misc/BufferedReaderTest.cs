using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tlabs.Misc.Tests {

  public class BufferedReaderTest {

    [Fact]
    public void BasicTest() {
      var br= new BufferedReader().BufferLine(null);
      Assert.Equal(-1, br.Read());
      Assert.Null(br.ReadLine());

      br= new BufferedReader().BufferLine(null);
      Assert.Null(br.ReadLine());
      Assert.Equal(-1, br.Read());
      Assert.Null(br.ReadLine());

      br= new BufferedReader().BufferLine("").BufferLine(null);
      Assert.NotNull(br.ReadLine());
      Assert.Null(br.ReadLine());
      Assert.Throws<InvalidOperationException>(() => br.BufferLine("more"));

      string restLine="-restLine";
      br= new BufferedReader().BufferLine("");
      Assert.NotNull(br.ReadLine());
      br.BufferLine("x"+restLine).BufferLine(null);
      Assert.Equal('x', br.Read());
      Assert.Equal(restLine, br.ReadLine());
      Assert.Null(br.ReadLine());
      Assert.Equal(-1, br.Read());
    }


    [Fact]
    public async Task AsyncTest() {
      var br= new BufferedReader().BufferLine("text");
      Assert.NotNull(await br.ReadLineAsync());
      var t= br.ReadLineAsync();
      await Task.Delay(10);
      br.BufferLine("more");
      br.Dispose();
      Assert.Equal("more", await t);
      Assert.Equal(0, await br.ReadAsync(new char[5], 0, 5));
    }


    [Fact]
    public async Task BlockingTest() {
      var br= new BufferedReader();
      var t= Task.Run(async () => {
        await Task.Delay(100);
        br.BufferLine("text-1");
        await Task.Delay(100);
        br.BufferLine("text-2");
      });
      Assert.Equal("text-1", br.ReadLine());
      Assert.Equal("text-2", br.ReadLine());
      await t;
    }

    [Fact]
    public async Task CancellationTest() {
      using var cts= new CancellationTokenSource(10);
      var br= new BufferedReader(cts.Token);
      Assert.Null(await br.ReadLineAsync());
      Assert.Null(br.ReadLine());
      Assert.Equal(-1, br.Read());
    }

    [Fact]
    public void AsyncThrowTest() {
      Task t;
      Assert.Throws<Exception>(() => {
        t= throwAsync(true);
      });
      t= throwAsync(false);
      t.AwaitWithTimeout(100);
    }

    Task throwAsync(bool throwSync) {
      if (throwSync) throw new Exception();
      return doAsync();
    }

    async Task doAsync() {
      await Task.Yield();
    }
  }
}