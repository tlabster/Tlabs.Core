using System;
using Xunit;

namespace Tlabs.Misc.Tests {

  public class OSInfoTest : IClassFixture<OSInfoTest.Fixture> {

    public sealed class Fixture {
      public int ResolveCount= 0;
      public Fixture() {
        OSInfo.OSPlatformResolved+= _ => ++this.ResolveCount;
      }

    }

    static Fixture fix;

    public OSInfoTest(Fixture fixture) => fix= fixture;

    [Fact]
    public void ResolveOSTest() {
      Assert.Equal(0, fix.ResolveCount);

      var os= OSInfo.CurrentPlatform;
      Assert.Equal(1, fix.ResolveCount);
      Assert.Same(os.ToString(), OSInfo.CurrentPlatform.ToString());
      Assert.Equal(1, fix.ResolveCount);
    }

  }

}