using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Dynamic.Tests {
  public class DynamicClassFactoryTests {
    static readonly DateTime TST_DATE= new DateTime(2002, 2, 2);

    public class TestBaseClass : ICloneable {
      public TestBaseClass() {
        this.BaseDate= TST_DATE;
      }
      public DateTime BaseDate {get; set; }
      public object Clone() => this.MemberwiseClone();
    }

    ITestOutputHelper tstout;
    public DynamicClassFactoryTests(ITestOutputHelper tstout) {
      this.tstout= tstout;
    }

    [Fact]
    public void BasicTest() {
      
      Assert.ThrowsAny<ArgumentException>(() => DynamicClassFactory.CreateType(null));

      var emptyProps= new List<DynamicProperty>();
      var emptyType= DynamicClassFactory.CreateType(emptyProps);
      Assert.IsAssignableFrom<Type>(emptyType);
      Assert.False(emptyType.IsGenericTypeDefinition);
      Assert.True(emptyType.Name.EndsWith("`0")); //no explicit name, zero properties
      Assert.Same(emptyType, DynamicClassFactory.CreateType(emptyProps));
      tstout.WriteLine(emptyType.Name);

      var myTypeName= "my--test-TYPE";
      var emptyType2= DynamicClassFactory.CreateType(emptyProps, typeof(object), myTypeName);
      Assert.NotSame(emptyType, emptyType2);
      Assert.Contains(myTypeName, emptyType2.Name); //has explicit name
      tstout.WriteLine(emptyType2.Name);

    }

    [Fact]
    public void DynamicTypeTest() {
      var props= new List<DynamicProperty> {
        new DynamicProperty("Text", typeof(string)),
        new DynamicProperty("Num", typeof(double)),
        new DynamicProperty("NullNum", typeof(float?)),
        new DynamicProperty("Bools", typeof(bool[]))
      };

      Type genericType= DynamicClassFactory.CreateType(props);
      testDynamicType(genericType);

      Type fixedNameType= DynamicClassFactory.CreateType(props, typeof(object), "DYN-propType");
      Assert.NotSame(genericType, fixedNameType);
      Assert.Equal(4, fixedNameType.GetProperties().Length);
      testDynamicType(fixedNameType);

      Type derivedGenericType= DynamicClassFactory.CreateType(props, typeof(TestBaseClass));
      dynamic obj= Activator.CreateInstance(derivedGenericType);
      Assert.Equal(5, derivedGenericType.GetProperties().Length);
      Assert.IsAssignableFrom<TestBaseClass>(obj);
      Assert.IsAssignableFrom<ICloneable>(obj);
      Assert.Equal(TST_DATE, obj.BaseDate);
      Assert.NotSame(obj, obj.Clone());
      Assert.NotSame(genericType, derivedGenericType);
      testDynamicType(derivedGenericType);


      Type derivedFnType= DynamicClassFactory.CreateType(props, typeof(TestBaseClass), "DYN-propType");
      obj= Activator.CreateInstance(derivedGenericType);
      Assert.Equal(5, derivedFnType.GetProperties().Length);
      Assert.IsAssignableFrom<TestBaseClass>(obj);
      Assert.IsAssignableFrom<ICloneable>(obj);
      Assert.Equal(TST_DATE, obj.BaseDate);
      Assert.NotSame(obj, obj.Clone());
      Assert.NotSame(fixedNameType, derivedFnType);
      testDynamicType(derivedFnType);
    }

    private void testDynamicType(Type dynType) {
      tstout.WriteLine(dynType.Name);
      tstout.WriteLine(dynType.FullName);
      Assert.NotEmpty(dynType.FullName);
      Assert.NotNull(dynType.GetProperty("Num"));
      Assert.Null(dynType.GetProperty("x y z"));

      dynamic obj= Activator.CreateInstance(dynType);
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