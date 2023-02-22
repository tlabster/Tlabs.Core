using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Misc.Tests {

  public class MiscTest {

    private readonly ITestOutputHelper tstout;

    public MiscTest(ITestOutputHelper output) {
      this.tstout= output;
    }

    [Fact]
    public void ExecContextTest() {
      ExecContext<MiscTest>.StartWith(this, () => {
        Assert.Same(this, ExecContext<MiscTest>.CurrentData);
      });
      Assert.Throws<InvalidOperationException>(()=> {
        var fail= ExecContext<MiscTest>.CurrentData;
      });
    }

    [Fact]
    public void PropertyTest() {
      var props= new Dictionary<string, object> {
        ["strProp"]= "test-string",
        ["boolProp"]= true,
        ["one"]= new Dictionary<string, object> {
          ["a"]= "one.a",
          ["b"]= "1.b"
        },
        ["strProp"]= "test-string",
      };

      Assert.Equal(props["strProp"], props.GetString("strProp"));
      Assert.True(props.GetBool("undefined", true));
      Assert.Throws<FormatException>(()=> {
        props.GetBool("strProp", true);
      });
      Assert.Null(props.GetString("boolProp"));

      Assert.Equal(props["strProp"], props.GetOrSet("strProp", "x"));
      Assert.Equal("new", props.GetOrSet("newProp", "new"));
      Assert.Equal("new", props.GetOrSet("newProp", "x"));

      object val= null;
      string key= null;
      Assert.True(props.TryResolveValue("strProp", out val, out key));
      Assert.Equal(props["strProp"], val);
      Assert.Equal("strProp", key);
      Assert.False(props.TryResolveValue("undefined", out val, out key));
      Assert.Null(val);
      Assert.Equal("undefined", key);

      Assert.True(props.TryResolveValue("one.a", out val, out key));
      Assert.Equal("one.a", val);
      Assert.Equal("a", key);

      Assert.False(props.TryResolveValue("one.undefined", out val, out key));
      Assert.Null(val);
      Assert.Equal("undefined", key);

      Assert.Equal("1.b", props.ResolvedProperty("[one.b]"));
      Assert.Equal("one.b", props.ResolvedProperty("one.b"));
      Assert.Equal("[one.b", props.ResolvedProperty("[one.b"));
      Assert.Equal("undefined", props.ResolvedProperty("undefined"));

      Assert.False(props.SetResolvedValue("boolProp.a", "xyz", out key));
      Assert.True(props.SetResolvedValue("some.more.prop", "xyz", out key));
      Assert.Equal("prop", key);
      Assert.True(props.TryResolveValue("some.more.prop", out val, out key));
      Assert.Equal("xyz", val);
      Assert.Equal("prop", key);
      Assert.True(props.SetResolvedValue("some.more", "x", out key));
      Assert.False(props.TryResolveValue("some.more.prop", out val, out key));

    }

    [Fact]
    public void LoadTypeTest() {
      var type= Safe.LoadType("Tlabs.Misc.Tests.MiscTest, Tlabs.Core.Tests", "test type");
      Assert.IsAssignableFrom<Type>(type);

      type= Safe.LoadType("Tlabs.Misc.Tests.MiscTest+GenericTest`1, Tlabs.Core.Tests & Tlabs.Misc.Tests.MiscTest, Tlabs.Core.Tests", "test type");
      Assert.IsAssignableFrom<Type>(type);

      Assert.Throws<AppConfigException>(() =>
        Safe.LoadType("Tlabs.Misc.Tests.MiscTest+GenericTest`1, Tlabs.Core.Tests & Tlabs.Misc.Tests.MiscTest+Dummy, Tlabs.Core.Tests", "test type")
      );

      Assert.Throws<AppConfigException>(() =>
        Safe.LoadType("Tlabs.Misc.Tests.XYZ, Tlabs.Core.Tests", "test type")
      );
    }

    [Fact]
    public void DictionaryListTest() {
      const int ONE= 1;
      const int N= 9;
      const int SUM= (N * (N+1)) / 2;
      var dlist= new DictionaryList<string, int>();

      Assert.Empty(dlist);
      Assert.Empty(dlist.Keys);
      Assert.Throws<KeyNotFoundException>(()=> dlist["x"]);

      dlist.Add(nameof(ONE), 1);
      Assert.NotEmpty(dlist);
      Assert.Equal(1, dlist.Keys.Count());
      Assert.NotEmpty(dlist[nameof(ONE)]);
      Assert.Equal(1, dlist[nameof(ONE)].First());

      for (var l= 1; l <= N; ++l)
        dlist.Add(nameof(SUM), l);
      Assert.Equal(2, dlist.Keys.Count());
      Assert.Equal(N, dlist[nameof(SUM)].Count());
      Assert.Equal(dlist.Values.Count(), dlist[nameof(SUM)].Count() + dlist[nameof(ONE)].Count());
      //Assert.Equal(SUM, dlist[nameof(SUM)].Sum());
      IEnumerable<int> lst;
      Assert.True(dlist.TryGetValue(nameof(SUM), out lst));
      Assert.Equal(SUM, lst.Sum());

      dlist.Remove(nameof(ONE));
      Assert.Single(dlist);
      Assert.False(dlist.TryGetValue(nameof(ONE), out lst));
      Assert.Null(lst);

      dlist.Clear();
      Assert.Empty(dlist);

    }
    public class GenericTest<T> where T : MiscTest {

    }
    public class Dummy { }
  }
}