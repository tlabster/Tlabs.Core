
using Xunit;

namespace Tlabs.Sync.Test {

  public class SynColletionTest {

    [Fact]
    public void IteratingRemoveTest() {

      var coll= new SyncCollection<string>(new string[] {"one", "two", "three"});
      foreach (var itm in coll.CollectionOf(i => i.StartsWith("t")))
        coll.Remove(itm);
      Assert.Equal(1, coll.Count);
    }
  }
}