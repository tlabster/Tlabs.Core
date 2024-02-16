using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Tlabs.Misc;
using Tlabs.Sync;

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
    public void DictionaryListTest() {
      IDictionaryListTest(new DictionaryList<string, int>());
    }

    [Fact]
    public void SyncDictionaryListTest() {
      IDictionaryListTest(new SyncDictionaryList<string, int>());
    }

    void IDictionaryListTest(IDictionaryList<string, int> dlist) {
      const int ONE= 1;
      const int N= 9;
      const int SUM= (N * (N+1)) / 2;

      Assert.Empty(dlist);
      Assert.Empty(dlist.Keys);
      Assert.Throws<KeyNotFoundException>(() => dlist["x"]);

      dlist.Add(nameof(ONE), 1);
      Assert.NotEmpty(dlist);
      Assert.Equal(1, dlist.Keys.Count());
      Assert.NotEmpty(dlist[nameof(ONE)]);
      Assert.Equal(1, dlist[nameof(ONE)].First());

      for (var l = 1; l <= N; ++l)
        dlist.Add(nameof(SUM), l);
      Assert.Equal(2, dlist.Keys.Count());
      Assert.Equal(1 + N, dlist.Count);
      Assert.Equal(N, dlist[nameof(SUM)].Count());
      Assert.Equal(dlist.Values.Count(), dlist[nameof(SUM)].Count() + dlist[nameof(ONE)].Count());
      Assert.Equal(SUM, dlist[nameof(SUM)].Sum());
      dlist.AddRange(nameof(SUM), new int[] { 0, 0 });
      Assert.Equal(SUM, dlist[nameof(SUM)].Sum());

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


    [Fact]
    public void LookupDictionaryTest() {
      var dict= new LookupDictionary<string, int>(k => -1) {
        ["one"]= 1,
        ["two"]= 2
      };

      Assert.NotEmpty(dict);
      Assert.NotEmpty(dict.Values);
      Assert.NotEmpty(dict.Keys);
      foreach (var p in dict) {
        Assert.Contains(p.Key, dict.Keys);
        Assert.Contains(p.Value, dict.Values);
        Assert.Contains(p, dict);
      }
      Assert.Equal(2, dict.Count);
      Assert.Equal(dict.Count, dict.Count());
      Assert.Equal(1, dict["one"]);
      dict["three"]= 3;
      Assert.True(dict.TryGetValue("three", out var i));
      Assert.Equal(3, i);

      Assert.Equal(-1, dict["undefined"]);
      Assert.False(dict.TryGetValue("undefined", out i));
      Assert.Equal(-1, dict.GetOrAdd("more"));
      Assert.True(dict.TryGetValue("more", out i));
      Assert.Equal(-1, i);

      Assert.Equal(dict, new LookupDictionary<string, int>(dict, k => -1));
      Assert.Equal(dict, new LookupDictionary<string, int>((IReadOnlyDictionary<string, int>)dict, k => -1));
      Assert.Equal(dict, new LookupDictionary<string, int>((IEnumerable<KeyValuePair<string, int>>)dict, k => -1));
    }

    [Fact]
    public void GenericEqualityTest() {
      var eq= new GenericEqualityComp<string>((a, b) => a == b, a => a.GetHashCode());

      Assert.Equal(StringComparer.Ordinal.Equals("abc", "abc"), eq.Equals("abc", "abc"));
      Assert.Equal(StringComparer.Ordinal.Equals("abc", "x"), eq.Equals("abc", "x"));
      Assert.Equal(StringComparer.Ordinal.GetHashCode("abc"), eq.GetHashCode("abc"));

    }


    [Fact]
    public void CollExtensionTest() {
      var set= new HashSet<string> {"one", "two", "three"};
      Assert.Same("one", set.GetOrAdd("one"));
      Assert.Equal(3, set.Count);
      Assert.Same("new", set.GetOrAdd("new"));
      Assert.Equal(4, set.Count);

      var sset= new SortedSet<string>(set, StringComparer.OrdinalIgnoreCase);
      Assert.True(set.ContentEquals(sset));
      Assert.Same("two", sset.GetOrAdd("tWo", StringComparer.OrdinalIgnoreCase));
      Assert.Equal(4, sset.Count);
      Assert.True(set.ContentEquals(sset, StringComparer.OrdinalIgnoreCase));

      Assert.True(ImmutableDictionary<string, int>.Empty.ContentEquals(ImmutableDictionary<string, int>.Empty));
      Assert.True(ImmutableDictionary<string, string>.Empty.ContentEquals(ImmutableDictionary<string, string>.Empty, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Array2dTest() {
      float[,] ary2d= { {0, 0.1f, 0.2f}, {1.0f, 1.1f, 1.2f} };
      var ary= new Array2DRowSlice<float>(ary2d, 1);
      Assert.Equal(3, ary.Count);
      Assert.Equal(1.0, ary[0]);
      Assert.Equal(new float[] { 1.0f, 1.1f, 1.2f }, ary);
    }
  }
}