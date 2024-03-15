using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using System.Collections;

namespace Tlabs.Dynamic.Tests {

  public class DynamicAccessorTest {

    static readonly decimal[] DECLST= new decimal[] { 3.141m, 2.71828m };
    static readonly string[] STRLST= new string[] { "test", "test2" };
    public class TstType {
      public int propInt { get; set; }
      public string propStr { get; set; }
      public bool propBool { get; set; }
      public decimal? propDec { get; set; }
      public IList<string> propList { get; set; }
      public IList<decimal> decList { get; set;}
      public IList<object> objList { get; set; }
      public int[] intArr { get; set; }
    }

    [Fact]
    public void BasicTest() {
      var obj= new {
        propInt= 123,
        propStr= "test",
        propBool= true,
        propDec= 1.23m,
        propList= STRLST,
        decList= DECLST
      };

      var propAcc= new DynamicAccessor(obj.GetType());

      Assert.Equal(obj.propInt, propAcc["propInt"].Get(obj));
      Assert.Equal(obj.propStr, propAcc["propStr"].Get(obj));
      Assert.Equal(obj.propBool, propAcc["propBool"].Get(obj));
      Assert.Equal(obj.propDec, propAcc["propDec"].Get(obj));
      Assert.Equal(obj.decList[0], ((IList<decimal>)propAcc["decList"].Get(obj))[0]);
      Assert.Equal(obj.decList[1], ((IList<decimal>)propAcc["decList"].Get(obj))[1]);

      propAcc["propInt"].Set(obj, 0);  //prop is read-only, this is NoOp.
      Assert.Equal(obj.propInt, propAcc["propInt"].Get(obj));

      Assert.Null(propAcc["xxx"].Get(obj));  //prop not defined

      var list= (IList<string>) propAcc["propList"].Get(obj);
      Assert.Equal("test", list.First());
    }

    [Fact]
    public void SetterTest() {
      var obj= new TstType {
        propInt= 123,
        propStr= "test",
        propBool= true,
        propDec= 1.23m,
        propList= STRLST,
        decList= DECLST
      };

      var propAcc= new DynamicAccessor(typeof(TstType));
      Assert.True(propAcc.Has("pROpInt"));  //case insensitive

      propAcc["propInt"].Set(obj, 999);
      Assert.Equal(999, propAcc["propInt"].Get(obj));

      propAcc["propStr"].Set(obj, "xxx");
      Assert.Equal("xxx", propAcc["propStr"].Get(obj));

      propAcc["propBool"].Set(obj, false);
      Assert.False((bool) propAcc["propBool"].Get(obj));

      propAcc["propDec"].Set(obj, 9.99m);
      Assert.Equal(9.99m, propAcc["propDec"].Get(obj));
      propAcc["propDec"].Set(obj, 123);
      Assert.Equal(123m, propAcc["propDec"].Get(obj));
      propAcc["propDec"].Set(obj, null);
      Assert.Null(propAcc["propDec"].Get(obj));

      var lst2= DECLST.ToArray();
      Assert.False(lst2.GetType() is IConvertible, "non IConvertible needed for this test");
      lst2[0]= 55;
      propAcc["decList"].Set(obj, lst2);
      Assert.Equal(lst2[0], ((IList<decimal>)propAcc["decList"].Get(obj))[0]);
      Assert.Equal(lst2[1], ((IList<decimal>)propAcc["decList"].Get(obj))[1]);

      propAcc["propList"].Set(obj, new List<string> { "test2" });
      var list= (List<string>) propAcc["propList"].Get(obj);
      Assert.Equal("test2", list.First());

      propAcc["propList"].Set(obj, new List<object> { "from obj" });   //type coercing from List<object> to List<string>
      list= (List<string>) propAcc["propList"].Get(obj);
      Assert.Equal("from obj", list.First());

      propAcc["intArr"].Set(obj, new List<object> { "1", 2, "3" });   //type coercing from List<object> to int[]
      var array= (int[])propAcc["intArr"].Get(obj);
      Assert.Equal(new int[] {1, 2, 3}, array);

      // Assigning an object of a type that does not implement IConvertible fails
      IEnumerable olst= new TestEnumerable(new object[] { "small", 123, 'x', 1.2345m });
      Assert.Throws<InvalidCastException>(()=> propAcc["objList"].Set(obj, olst));
    }

    [Fact]
    public void DictionaryTest() {
      var obj= new TstType {
        propInt= 123,
        propStr= "test",
        propBool= true,
        propDec= 1.23m,
        decList= DECLST
      };

      var propAcc= new DynamicAccessor(typeof(TstType));
      var props= propAcc.ToDictionary(obj);
      Assert.True(props.Count > 3);
      Assert.False(props.IsReadOnly);
      Assert.NotEmpty(props.Keys);
      Assert.NotEmpty(props.Values);
      Assert.Contains(new KeyValuePair<string, object>("propInt", 123), props);
      Assert.False(props.TryGetValue("undefined", out var x));
      var array= new KeyValuePair<string, object>[props.Count];
      props.CopyTo(array, 0);
      Assert.Equal(props, array);

      foreach(var prop in props) {
        Assert.True(props.ContainsKey(prop.Key));
        Assert.True(props.TryGetValue(prop.Key.ToUpperInvariant(), out var o));
        Assert.Equal(propAcc[prop.Key].Get(obj), prop.Value);
        Assert.Equal(propAcc[prop.Key].Get(obj), props[prop.Key]);
      }

      props["propInt"]= 999;
      Assert.Equal(999, propAcc["propInt"].Get(obj));

      props["propStr"]= "xxx";
      Assert.Equal("xxx", propAcc["propStr"].Get(obj));

      props["propBool"]= false;
      Assert.False((bool) propAcc["propBool"].Get(obj));

      props["propDec"]= 9.99m;
      Assert.Equal(9.99m, propAcc["propDec"].Get(obj));
      props["propDec"]= null;
      Assert.Null(propAcc["propDec"].Get(obj));
      Assert.Null(obj.propDec);

      props["decList"]= null;
      Assert.Null(propAcc["decList"].Get(obj));
      Assert.Null(obj.decList);

      var lst2= DECLST.ToArray();
      Assert.False(lst2.GetType() is IConvertible, "non IConvertible needed for this test");
      lst2[0]= 55;
      props["decList"]= lst2;
      Assert.Equal(lst2[0], ((IList<decimal>)propAcc["decList"].Get(obj))[0]);
      Assert.Equal(lst2[0], obj.decList[0]);
      Assert.Equal(lst2[1], ((IList<decimal>)propAcc["decList"].Get(obj))[1]);
      Assert.Equal(lst2[1], obj.decList[1]);


      var enm= new TestEnumerable(lst2);
      Assert.False(enm.GetType() is IConvertible, "non IConvertible needed for this test");
      Assert.Throws<InvalidCastException>(() => props["decList"]= enm);
      Assert.Equal(lst2[0], ((IList<decimal>)propAcc["decList"].Get(obj))[0]);
      Assert.Equal(lst2[0], obj.decList[0]);
      Assert.Equal(lst2[1], ((IList<decimal>)propAcc["decList"].Get(obj))[1]);
      Assert.Equal(lst2[1], obj.decList[1]);

      Assert.Throws<InvalidOperationException>(() => props.Clear());
      Assert.Throws<InvalidOperationException>(() => props.Remove("any"));
      Assert.Throws<InvalidOperationException>(() => ((ICollection<KeyValuePair<string, object>>)props).Remove(new KeyValuePair<string, object>("any", 0)));
      Assert.Throws<InvalidOperationException>(() => props.Add("any", null));
    }

    [Fact]
    public void ValueDictionaryTest() {
      var obj= new TstType {
        propInt= 123
      };

      var propAcc= new DynamicAccessor(typeof(TstType));
      var props= propAcc.ToValueDictionary(obj);

      props.Add("add", 123);
      Assert.Equal(123, props["add"]);

      props["propInt"]= 999;
      Assert.NotEqual(999, propAcc["propInt"].Get(obj));
      Assert.Equal(123, propAcc["propInt"].Get(obj));

    }
  }

  class TestEnumerable : IEnumerable {
    IEnumerable enm;

    public TestEnumerable(IEnumerable enm) {
      this.enm= enm;
    }
    public IEnumerator GetEnumerator() {
      foreach(var e in enm)
        yield return e;
    }
  }
}