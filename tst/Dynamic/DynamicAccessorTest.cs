using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using System.Collections;

namespace Tlabs.Dynamic.Tests {

  public class DynamicAccessorTest {

    static readonly decimal[] DECLST= new decimal[] { 3.141m, 2.71828m };
    public class TstType {
      public int propInt { get; set; }
      public string propStr { get; set; }
      public bool propBool { get; set; }
      public decimal? propDec { get; set; }
      public List<string> propList { get; set; }
      public IList<decimal> decList { get; set;}
    }

    [Fact]
    public void BasicTest() {
      var obj= new {
        propInt= 123,
        propStr= "test",
        propBool= true,
        propDec= 1.23m,
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
    }

    [Fact]
    public void SetterTest() {
      var obj= new TstType {
        propInt= 123,
        propStr= "test",
        propBool= true,
        propDec= 1.23m
      };

      var propAcc= new DynamicAccessor(typeof(TstType));

      propAcc["propInt"].Set(obj, 999);
      Assert.Equal(999, propAcc["propInt"].Get(obj));

      propAcc["propStr"].Set(obj, "xxx");
      Assert.Equal("xxx", propAcc["propStr"].Get(obj));

      propAcc["propBool"].Set(obj, false);
      Assert.False((bool) propAcc["propBool"].Get(obj));

      propAcc["propDec"].Set(obj, 9.99m);
      Assert.Equal(9.99m, propAcc["propDec"].Get(obj));
      propAcc["propDec"].Set(obj, null);
      Assert.Equal(null, propAcc["propDec"].Get(obj));

      var lst2= DECLST.ToArray();
      Assert.False(lst2.GetType() is IConvertible, "non IConvertible needed for this test");
      lst2[0]= 55;
      propAcc["decList"].Set(obj, lst2);
      Assert.Equal(lst2[0], ((IList<decimal>)propAcc["decList"].Get(obj))[0]);
      Assert.Equal(lst2[1], ((IList<decimal>)propAcc["decList"].Get(obj))[1]);

      propAcc["propList"].Set(obj, new List<string> { "test2" });
      var list= (List<string>) propAcc["propList"].Get(obj);
      Assert.Equal("test2", list.First());

      // Assigning an object of a type that does not implement IConvertible fails
      string json = @"[
        'Small',
        'Medium',
        'Large'
      ]";

      Newtonsoft.Json.Linq.JArray a= Newtonsoft.Json.Linq.JArray.Parse(json);
      propAcc["propList"].Set(obj, a);
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
      foreach(var prop in props) {
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
      Assert.Equal(null, propAcc["propDec"].Get(obj));
      Assert.Equal(null, obj.propDec);

      props["decList"]= null;
      Assert.Equal(null, propAcc["decList"].Get(obj));
      Assert.Equal(null, obj.decList);

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
      props["decList"]= enm;
      Assert.Equal(lst2[0], ((IList<decimal>)propAcc["decList"].Get(obj))[0]);
      Assert.Equal(lst2[0], obj.decList[0]);
      Assert.Equal(lst2[1], ((IList<decimal>)propAcc["decList"].Get(obj))[1]);
      Assert.Equal(lst2[1], obj.decList[1]);
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