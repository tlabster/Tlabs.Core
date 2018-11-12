using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Tlabs.Dynamic;
using Xunit;

namespace Tlabs.DynamicClass.Tests {
  public partial class DynamicClassFactoryTests {
    [Fact]
    public void DynamicAttributesTest() {

      var dynProps= new List<DynamicProperty> {new DynamicProperty("Test", typeof(string), new List<DynamicAttribute> { new DynamicAttribute(typeof(ConditionalAttribute), new [] { "Debug" } ) }) };

      Type createdType = DynamicClassFactory.CreateType(dynProps);

      ConditionalAttribute attr = createdType.GetProperties()[0].GetCustomAttributes(typeof(ConditionalAttribute), false).Cast<ConditionalAttribute>().FirstOrDefault();
      Assert.Equal("Debug", attr.ConditionString);
    }
  }
}