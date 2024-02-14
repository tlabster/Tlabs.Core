using System;
using System.Threading;
using System.Threading.Tasks;

using Tlabs.Sync;

using Xunit;

namespace Tlabs.Misc.Tests {

  public class OSInfoTest : IClassFixture<OSInfoTest.Fixture> {

    public sealed class Fixture {
      public SyncMonitor<int> ResCnt= new();
      public Fixture() {
        OSInfo.OSPlatformResolved+= _ => {
          ResCnt.Signal(++ResCnt.Value);
        };
      }

    }

    static Fixture fix;

    public OSInfoTest(Fixture fixture) => fix= fixture;

    [Fact]
    public void ResolveOSTest() {
      Assert.Equal(0, fix.ResCnt.Value);

      var os= OSInfo.CurrentPlatform.ToString();
      Assert.NotEmpty(os);
      // Assert.Equal(1, fix.ResCnt.WaitForSignal(500));
      // Assert.Same(os, OSInfo.CurrentPlatform.ToString());
      // Assert.Equal(1, fix.ResCnt.Value);
    }

  }

}