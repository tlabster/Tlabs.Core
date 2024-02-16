
using Xunit;

namespace Tlabs.Sync.Test {

  public class SynColletionTest {

    [Fact]
    public void BasicTest() {
      var coll= new SyncCollection<string>(new string[] {"one", "two", "three"});

      Assert.NotEmpty(coll);
      Assert.Equal(3, coll.Count);
      foreach (var itm in coll) {
        Assert.Contains(itm, coll);
      }
      Assert.True(coll.Contains(itm => itm.EndsWith("ee")));

      coll.Add("more");
      coll.AddRange(new string[] { "even", "some" });
      Assert.False(coll.Remove("none"));
      Assert.Equal(6, coll.Count);
      coll.Clear();
      Assert.Empty(coll);
    }

    [Fact]
    public void IteratingRemoveTest() {
      var coll= new SyncCollection<string>(new string[] {"one", "two", "three"});

      foreach (var itm in coll.CollectionOf(i => i.StartsWith("t")))
        coll.Remove(itm);
      Assert.Equal(1, coll.Count);
    }
  }
}