using System;
using Xunit;

namespace Tlabs.Dynamic.Tests {

  public class DynamicAccessorTest {
    public class TstType {
      public int propInt { get; set; }
      public string propStr { get; set; }
      public bool propBool { get; set; }
      public decimal propDec { get; set; }
    }

    [Fact]
    public void BasicTest() {
      var obj= new {
        propInt= 123,
        propStr= "test",
        propBool= true,
        propDec= 1.23m
      };

      var propAcc= new DynamicAccessor(obj.GetType());

      Assert.Equal(obj.propInt, propAcc["propInt"].Get(obj));
      Assert.Equal(obj.propStr, propAcc["propStr"].Get(obj));
      Assert.Equal(obj.propBool, propAcc["propBool"].Get(obj));
      Assert.Equal(obj.propDec, propAcc["propDec"].Get(obj));

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
      Assert.Equal(false, propAcc["propBool"].Get(obj));

      propAcc["propDec"].Set(obj, 9.99m);
      Assert.Equal(9.99m, propAcc["propDec"].Get(obj));
    }

    [Fact]
    public void DictionaryTest() {
      var obj= new TstType {
        propInt= 123,
        propStr= "test",
        propBool= true,
        propDec= 1.23m
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
      Assert.Equal(false, propAcc["propBool"].Get(obj));

      props["propDec"]= 9.99m;
      Assert.Equal(9.99m, propAcc["propDec"].Get(obj));

    }
  }
}