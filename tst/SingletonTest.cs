using System;
using Xunit;

namespace Tlabs.Misc.Tests {

  public class SingletonTest : IClassFixture<SingletonTest.Fixture> {

    public class Fixture {
      public int CreationCount= 0;
    }

    static Fixture fix;

    public SingletonTest(Fixture fixture) => fix= fixture;

    [Fact]
    public void CreationTest() {
      Assert.Equal(0, fix.CreationCount);

      var tst= Singleton<TstClass>.Instance;
      Assert.Equal(1, fix.CreationCount);
      Assert.Same(tst, Singleton<TstClass>.Instance);
      Assert.Equal(1, fix.CreationCount);
    }

    public class TstClass {
      public TstClass() {
        ++fix.CreationCount;
      }
    }
  }

}