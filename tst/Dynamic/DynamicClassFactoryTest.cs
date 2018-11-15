using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Tlabs.Dynamic.Tests {
  public partial class DynamicClassFactoryTests {

    [Fact]
    public void BasicTest() {
      var props= new List<DynamicProperty> {
        new DynamicProperty("Text", typeof(string)),
        new DynamicProperty("Num", typeof(double)),
        new DynamicProperty("NullNum", typeof(float?)),
        new DynamicProperty("Bools", typeof(bool[]))
      };

      Type createdType= DynamicClassFactory.CreateType(props);
      Assert.NotEmpty(createdType.FullName);
      Assert.Equal(4, createdType.GetProperties().Length);
      Assert.NotNull(createdType.GetProperty("Num"));
      Assert.Null(createdType.GetProperty("x y z"));

      dynamic obj= Activator.CreateInstance(createdType);
      var num= 1.23;
      obj.Num= num;
      Assert.Equal(num, obj.Num);
      
      float? noNum= obj.NullNum;
      Assert.False(noNum.HasValue);

      Assert.ThrowsAny<Exception>(() => obj.undefined= 0);
    }

    [Fact]
    public void DynamicAttributesTest() {

      var dynProps= new List<DynamicProperty> {new DynamicProperty("Test", typeof(string), new List<DynamicAttribute> { new DynamicAttribute(typeof(ConditionalAttribute), new [] { "Debug" } ) }) };

      Type createdType = DynamicClassFactory.CreateType(dynProps);

      ConditionalAttribute attr = createdType.GetProperties()[0].GetCustomAttributes(typeof(ConditionalAttribute), false).Cast<ConditionalAttribute>().FirstOrDefault();
      Assert.Equal("Debug", attr.ConditionString);
    }
  }
}