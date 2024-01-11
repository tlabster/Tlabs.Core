using System;
using System.Threading.Tasks;

using Xunit;

namespace Tlabs.Misc.Tests {

  public class OSInfoTest : IClassFixture<OSInfoTest.Fixture> {

    public sealed class Fixture {
      int resCnt= 0;
      public int ResolveCount() {
        lock(this) return resCnt;
      }
      public Fixture() {
        OSInfo.OSPlatformResolved+= _ => { lock(this) ++resCnt; };
      }

    }

    static Fixture fix;

    public OSInfoTest(Fixture fixture) => fix= fixture;

    [Fact]
    public void ResolveOSTest() {
      Assert.Equal(0, fix.ResolveCount());
      var os= OSInfo.CurrentPlatform.ToString();
      Assert.NotEmpty(os);
      Assert.Same(os, OSInfo.CurrentPlatform.ToString());
      Assert.Equal(1, fix.ResolveCount());
    }

  }

}