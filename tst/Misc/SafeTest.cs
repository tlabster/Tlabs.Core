using System;

using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Tlabs.Misc.Tests {

  public class SafeTest {

    public class GenericTest<T> where T : SafeTest {
      public T? t;
    }
    public class Dummy {
      public int x;
    }

    private readonly ITestOutputHelper tstout;
    public SafeTest(ITestOutputHelper output) {
      this.tstout= output;
    }

    [Fact]
    public void LoadTypeTest() {
      var type= Safe.LoadType("Tlabs.Misc.Tests.SafeTest, Tlabs.Core.Tests", "test type");
      Assert.IsAssignableFrom<Type>(type);

      type= Safe.LoadType("Tlabs.Misc.Tests.SafeTest+Dummy, Tlabs.Core.Tests", "test type");
      Assert.IsAssignableFrom<Type>(type);

      type= Safe.LoadType("Tlabs.Misc.Tests.SafeTest+GenericTest`1[[Tlabs.Misc.Tests.SafeTest, Tlabs.Core.Tests]], Tlabs.Core.Tests", "test type");
      Assert.IsAssignableFrom<Type>(type);

      type= Safe.LoadType("Tlabs.Misc.Tests.SafeTest+GenericTest`1, Tlabs.Core.Tests & Tlabs.Misc.Tests.SafeTest, Tlabs.Core.Tests", "test type");
      Assert.IsAssignableFrom<Type>(type);

      Assert.Throws<AppConfigException>(() =>
        Safe.LoadType("Tlabs.Misc.Tests.SafeTest+GenericTest`1, Tlabs.Core.Tests & Tlabs.Misc.Tests.SafeTest+Dummy, Tlabs.Core.Tests", "test type")
      );

      Assert.Throws<AppConfigException>(() =>
        Safe.LoadType("Tlabs.Misc.Tests.XYZ, Tlabs.Core.Tests", "test type")
      );
    }

    [Fact]
    public void CompareExchangeTest() {
      var s= "replace me";
      Assert.Equal("replace me", Safe.CompareExchange(ref s, "replace me", ()=>"new"));
      Assert.Equal("new", s);
      Assert.Equal("new", Safe.CompareExchange(ref s, "replace me", () => throw new Exception("must not be called")));
      Assert.Equal("new", s);

      string? s0= null;
      Assert.Null(Safe.CompareExchange(ref s0, null, ()=> "new"));
    }

    static int disposeCnt;
    sealed class Disposable : IDisposable {
      public void Dispose() => ++disposeCnt;
    }
    sealed class Container : IDisposable {
      public Disposable? ToBeDisposed;
      public Container(Disposable d) => ToBeDisposed= d;
      public void Dispose() => ++disposeCnt;
    }

    [Fact]
    public void AllocTest() {
      disposeCnt= 0;
      Safe.Allocated<Container, Disposable>(d => new Container(d));
      Assert.Equal(0, disposeCnt);
      Assert.Throws<Exception>(()=> Safe.Allocated<Container, Disposable>(d => throw new Exception()));
      Assert.Equal(1, disposeCnt);

      disposeCnt= 0;
      Safe.Allocated<Container, Disposable>(
        ()=> new Disposable(),
        (d)=> new Container(d),
        (d, t) => {}
      );
      Assert.Equal(0, disposeCnt);
      Assert.Throws<Exception>(() => Safe.Allocated<Container, Disposable>(
        () => new Disposable(),
        (d) => throw new Exception(),
        (d, t) => { }
      ));
      Assert.Equal(1, disposeCnt);
      Assert.Throws<Exception>(()=> Safe.Allocated<Container, Disposable>(
        () => new Disposable(),
        (d) => new Container(d),
        (d, t) => throw new Exception()
      ));
      Assert.Equal(3, disposeCnt);
    }

  }

}