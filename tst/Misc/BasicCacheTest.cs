using System.Linq;
using Xunit;

namespace Tlabs.Misc.Tests {

  public class CacheTest {

    [Fact]
    public void BasicCacheTest() {
      var cache= new BasicCache<string, string>();
      test(cache);
    }

    [Fact]
    public void ConcurrentCacheTest() {
      var cache= new LookupCache<string, string>();
      test(cache);
    }

    private void test(ICache<string, string> cache) {
      Assert.Null(cache["undefined"]);
      Assert.Equal("val01", cache["key01"]= "val01");
      Assert.Equal("val01", cache["key01", () => {
        Assert.True(false, "Must not invoke getValue");
        return "invalid";
      }
      ]);
      Assert.Equal("val02", cache["key02"]= "val02");
      Assert.Equal("val02", cache["key02"]);
      Assert.Equal("val02.1", cache["key02"]= "val02.1"); //overwrite
      Assert.Equal("val02.1", cache["key02"]);
      Assert.Equal("val03", cache["key03", () => "val03"]);
      Assert.Equal("val03", cache["key03"]);
      Assert.Equal("val03", cache.Evict("key03"));
      Assert.Null(cache.Evict("key03"));
      Assert.Equal("val03", cache["key03"]= "val03");
      Assert.Equal("val03", cache.Evict("key03"));
      Assert.NotEmpty(cache.Entries);
      Assert.NotEmpty(cache.Entries.Where(e => e.Value.StartsWith("val0")));
    }
  }
}