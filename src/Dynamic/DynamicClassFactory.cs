using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Tlabs.Dynamic {
  /// <summary>
  /// A factory to create dynamic classes, based on <see href="http://stackoverflow.com/questions/29413942/c-sharp-anonymous-object-with-properties-from-dictionary" />.
  /// </summary>
  public static class DynamicClassFactory {
    // EmptyTypes is used to indicate that we are looking for someting without any parameters. 
    private static readonly Type[] EmptyTypes= new Type[0];

    private static readonly Misc.BasicCache<string, Type> typeCache= new Misc.BasicCache<string, Type>();
    
    private static readonly ModuleBuilder ModuleBuilder;

    // Some objects we cache
    private static readonly CustomAttributeBuilder CompilerGeneratedAttrib= new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(EmptyTypes), new object[0]);
    private static readonly CustomAttributeBuilder DebuggerBrowsableAttrib= new CustomAttributeBuilder(typeof(DebuggerBrowsableAttribute).GetConstructor(new[] { typeof(DebuggerBrowsableState) }), new object[] { DebuggerBrowsableState.Never });
    private static readonly CustomAttributeBuilder DebuggerHiddenAttrib= new CustomAttributeBuilder(typeof(DebuggerHiddenAttribute).GetConstructor(EmptyTypes), new object[0]);

    private static readonly ConstructorInfo DefaultCtor= typeof(object).GetConstructor(EmptyTypes);
    private static readonly MethodInfo ToStringMethod= typeof(object).GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public);

    private static readonly ConstructorInfo StringBuilderCtor= typeof(StringBuilder).GetConstructor(EmptyTypes);
    private static readonly MethodInfo StringBuilderAppendString= typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) });
    private static readonly MethodInfo StringBuilderAppendObject= typeof(StringBuilder).GetMethod("Append", new[] { typeof(object) });

    private static readonly Type EqualityComparer= typeof(EqualityComparer<>);
    private static readonly Type EqualityComparerGenericArgument= EqualityComparer.GetGenericArguments()[0];

    private static readonly MethodInfo EqualityComparerDefault= EqualityComparer.GetMethod("get_Default", BindingFlags.Static | BindingFlags.Public);
    private static readonly MethodInfo EqualityComparerEquals= EqualityComparer.GetMethod("Equals", new[] { EqualityComparerGenericArgument, EqualityComparerGenericArgument });
    private static readonly MethodInfo EqualityComparerGetHashCode= EqualityComparer.GetMethod("GetHashCode", new[] { EqualityComparerGenericArgument });

    private static readonly Type DefaultBaseType= typeof(object);
    private static readonly string typeNamePrefix= "<>__" + nameof(DynamicClassFactory) + "__";
    private static int sequence= 0;

    class PropertyDef {
      public string Name;
      public Type Type;
      public IList<DynamicAttribute> Attributes;
      public FieldBuilder Field;
    }

    /// <summary>
    /// Initializes the <see cref="DynamicClassFactory"/> class.
    /// </summary>
    static DynamicClassFactory() {
      var assemblyName= new AssemblyName(typeof(DynamicClassFactory).FullName + ", Version=1.0.0.0");
      var assemblyBuilder= AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
      ModuleBuilder= assemblyBuilder.DefineDynamicModule(assemblyName.Name);
    }

    /// <summary>Dynamically creates a new class type with a given set of public properties.</summary>
    /// <returns><see cref="Type"/> of the newly created class.</returns>
    /// <remarks>
    /// If a data class with an identical sequence of properties has already been created, the System.Type object for this already created class is returned.        
    /// The generated classes
    /// <list>
    /// <item><description>implement private instance variables and read/write property accessors for the specified properties.</description></item>
    /// <item><description>override the Equals and GetHashCode members to implement by-value equality.</description></item>
    /// <item><description>are created in an in-memory assembly in the current application domain.</description></item>
    /// <item><description>inherit from <see cref="Object"/>  if no <paramref name="parentType"/> is specified.paramref name="parentType"</description></item>
    /// <item><description>get a automatically generated names that should be considered private
    /// (the names will be unique within the application).</description></item>
    /// </list>
    /// Note that once created, a generated class stays in memory for the lifetime of the current application. There is currently no way to unload a dynamically created class.
    /// </remarks>
    /// <param name="properties">The DynamicProperties</param>
    /// <param name="parentType">The type to inherit from, defaults to null</param>
    /// <param name="typeName">optional typeName</param>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// DynamicProperty[] props= new DynamicProperty[] { new DynamicProperty("Name", typeof(string)), new DynamicProperty("Birthday", typeof(DateTime)) };
    /// Type type= DynamicClassFactory.CreateType(props);
    /// dynamic dynamicClass= Activator.CreateInstance(type) as DynamicClass;
    /// dynamicClass.Name= "Albert";
    /// dynamicClass.Birthday= new DateTime(1879, 3, 14);
    /// Console.WriteLine(dynamicClass);
    /// ]]>
    /// </code>
    /// </example>
    public static Type CreateType(IList<DynamicProperty> properties, Type parentType= null, string typeName= null) {
      if (null == properties) throw new ArgumentNullException(nameof(properties));

      string typeKey= typeName ?? generateTypeKey(properties);
      Func<Type> createNew= () => createNewType(properties, parentType, typeName);

      return typeCache[typeKey, createNew];
    }

    private static Type createNewType(IList<DynamicProperty> properties, Type parentType, string typeName) {
      Type type;
      string seq= $":{Interlocked.Increment(ref sequence)}";
      var genericType= string.IsNullOrEmpty(typeName);

      string name=   typeNamePrefix
                   + (genericType ? $"{seq}`{properties.Count}" : (typeName + seq.ToString()));

      TypeBuilder tb= ModuleBuilder.DefineType(name,
                                                 TypeAttributes.AnsiClass
                                               | TypeAttributes.Public
                                               | TypeAttributes.Class
                                               | TypeAttributes.AutoLayout
                                               | TypeAttributes.BeforeFieldInit,
                                               parentType ?? DefaultBaseType);
      tb.SetCustomAttribute(CompilerGeneratedAttrib);


      var propDefs= properties.Select(prop => new PropertyDef { Name= prop.Name, Type= prop.Type, Attributes= prop.Attributes }).ToList();

      if (genericType) {
        var genericParams=   propDefs.Count > 0
                           ? tb.DefineGenericParameters(propDefs.Select(prop => $"TPar__<{prop.Name}>").ToArray())
                           : new GenericTypeParameterBuilder[0];
        for (int l= 0; l < genericParams.Length; ++l) {
          var tp= genericParams[l];
          tp.SetCustomAttribute(CompilerGeneratedAttrib);
          propDefs[l].Type= tp; //use generic type parameter type
        }
      }

      // There are two loops here because we want to have all the getter methods before all the other methods
      foreach (var prop in propDefs) {
        var propField= prop.Field= tb.DefineField($"<{prop.Name}>i__Field", prop.Type, FieldAttributes.Private | FieldAttributes.InitOnly);
        propField.SetCustomAttribute(DebuggerBrowsableAttrib);

        PropertyBuilder property= tb.DefineProperty(prop.Name, PropertyAttributes.None, CallingConventions.HasThis, prop.Type, EmptyTypes);

        // getter
        MethodBuilder getter= tb.DefineMethod($"get_{prop.Name}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, CallingConventions.HasThis, prop.Type, null);
        getter.SetCustomAttribute(CompilerGeneratedAttrib);
        ILGenerator ilgeneratorGetter= getter.GetILGenerator();
        ilgeneratorGetter.Emit(OpCodes.Ldarg_0);
        ilgeneratorGetter.Emit(OpCodes.Ldfld, propField);
        ilgeneratorGetter.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);

        // setter
        MethodBuilder setter= tb.DefineMethod($"set_{prop.Name}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, CallingConventions.HasThis, null, new[] { prop.Type });
        setter.SetCustomAttribute(CompilerGeneratedAttrib);

        // workaround for https://github.com/dotnet/corefx/issues/7792
        setter.DefineParameter(1, ParameterAttributes.In, "value");

        ILGenerator ilgeneratorSetter= setter.GetILGenerator();
        ilgeneratorSetter.Emit(OpCodes.Ldarg_0);
        ilgeneratorSetter.Emit(OpCodes.Ldarg_1);
        ilgeneratorSetter.Emit(OpCodes.Stfld, propField);
        ilgeneratorSetter.Emit(OpCodes.Ret);
        property.SetSetMethod(setter);

        /* Apply custom property attributes:
          */
        if (null != prop.Attributes) foreach (var attr in prop.Attributes) {
          property.SetCustomAttribute(
            new CustomAttributeBuilder(attr.Type.GetConstructor(attr.Parameters.Select(o => o?.GetType()).ToArray()),
                                      attr.Parameters)
          );
        }
      }

      // ToString()
      MethodBuilder toString= tb.DefineMethod("ToString", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, typeof(string), EmptyTypes);
      toString.SetCustomAttribute(DebuggerHiddenAttrib);
      ILGenerator ilgeneratorToString= toString.GetILGenerator();
      ilgeneratorToString.DeclareLocal(typeof(StringBuilder));
      ilgeneratorToString.Emit(OpCodes.Newobj, StringBuilderCtor);
      ilgeneratorToString.Emit(OpCodes.Stloc_0);

      // Equals
      MethodBuilder equals= tb.DefineMethod("Equals", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, typeof(bool), new[] { typeof(object) });
      equals.SetCustomAttribute(DebuggerHiddenAttrib);
      equals.DefineParameter(1, ParameterAttributes.In, "value");

      ILGenerator ilgeneratorEquals= equals.GetILGenerator();
      ilgeneratorEquals.DeclareLocal(tb);
      ilgeneratorEquals.Emit(OpCodes.Ldarg_1);
      ilgeneratorEquals.Emit(OpCodes.Isinst, tb);
      ilgeneratorEquals.Emit(OpCodes.Stloc_0);
      ilgeneratorEquals.Emit(OpCodes.Ldloc_0);

      Label equalsLabel= ilgeneratorEquals.DefineLabel();

      // GetHashCode()
      MethodBuilder getHashCode= tb.DefineMethod("GetHashCode", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, typeof(int), EmptyTypes);
      getHashCode.SetCustomAttribute(DebuggerHiddenAttrib);
      ILGenerator ilgeneratorGetHashCode= getHashCode.GetILGenerator();
      ilgeneratorGetHashCode.DeclareLocal(typeof(int));

      if (properties.Count == 0) {
        ilgeneratorGetHashCode.Emit(OpCodes.Ldc_I4_0);
      }
      else {
        // As done by Roslyn
        // Note that initHash can vary, because string.GetHashCode() isn't "stable" for different compilation of the code
        int initHash= 0;
        foreach (var prop in properties)
          initHash= unchecked(initHash * (-1521134295) + prop.Name.GetHashCode());

        // Note that the CSC seems to generate a different seed for every anonymous class
        ilgeneratorGetHashCode.Emit(OpCodes.Ldc_I4, initHash);
      }

      var first= true;
      foreach (var prop in propDefs) {
        Type equalityComparerT= EqualityComparer.MakeGenericType(prop.Type);

        // Equals()
        MethodInfo equalityComparerTDefault= TypeBuilder.GetMethod(equalityComparerT, EqualityComparerDefault);
        MethodInfo equalityComparerTEquals= TypeBuilder.GetMethod(equalityComparerT, EqualityComparerEquals);

        // Illegal one-byte branch at position: 9. Requested branch was: 143.
        // So replace OpCodes.Brfalse_S to OpCodes.Brfalse
        ilgeneratorEquals.Emit(OpCodes.Brfalse, equalsLabel);
        ilgeneratorEquals.Emit(OpCodes.Call, equalityComparerTDefault);
        ilgeneratorEquals.Emit(OpCodes.Ldarg_0);
        ilgeneratorEquals.Emit(OpCodes.Ldfld, prop.Field);
        ilgeneratorEquals.Emit(OpCodes.Ldloc_0);
        ilgeneratorEquals.Emit(OpCodes.Ldfld, prop.Field);
        ilgeneratorEquals.Emit(OpCodes.Callvirt, equalityComparerTEquals);

        // GetHashCode();
        MethodInfo equalityComparerTGetHashCode= TypeBuilder.GetMethod(equalityComparerT, EqualityComparerGetHashCode);
        ilgeneratorGetHashCode.Emit(OpCodes.Stloc_0);
        ilgeneratorGetHashCode.Emit(OpCodes.Ldc_I4, -1521134295);
        ilgeneratorGetHashCode.Emit(OpCodes.Ldloc_0);
        ilgeneratorGetHashCode.Emit(OpCodes.Mul);
        ilgeneratorGetHashCode.Emit(OpCodes.Call, equalityComparerTDefault);
        ilgeneratorGetHashCode.Emit(OpCodes.Ldarg_0);
        ilgeneratorGetHashCode.Emit(OpCodes.Ldfld, prop.Field);
        ilgeneratorGetHashCode.Emit(OpCodes.Callvirt, equalityComparerTGetHashCode);
        ilgeneratorGetHashCode.Emit(OpCodes.Add);

        // ToString();
        ilgeneratorToString.Emit(OpCodes.Ldloc_0);
        ilgeneratorToString.Emit(OpCodes.Ldstr, first ? $"{{ {prop.Name}= " : $", {prop.Name}= ");
        ilgeneratorToString.Emit(OpCodes.Callvirt, StringBuilderAppendString);
        ilgeneratorToString.Emit(OpCodes.Pop);
        ilgeneratorToString.Emit(OpCodes.Ldloc_0);
        ilgeneratorToString.Emit(OpCodes.Ldarg_0);
        ilgeneratorToString.Emit(OpCodes.Ldfld, prop.Field);
        ilgeneratorToString.Emit(OpCodes.Box, prop.Type);
        ilgeneratorToString.Emit(OpCodes.Callvirt, StringBuilderAppendObject);
        ilgeneratorToString.Emit(OpCodes.Pop);
        first= false;
      }

      // Equals()
      if (properties.Count == 0) {
        ilgeneratorEquals.Emit(OpCodes.Ldnull);
        ilgeneratorEquals.Emit(OpCodes.Ceq);
        ilgeneratorEquals.Emit(OpCodes.Ldc_I4_0);
        ilgeneratorEquals.Emit(OpCodes.Ceq);
      }
      else {
        ilgeneratorEquals.Emit(OpCodes.Ret);
        ilgeneratorEquals.MarkLabel(equalsLabel);
        ilgeneratorEquals.Emit(OpCodes.Ldc_I4_0);
      }

      ilgeneratorEquals.Emit(OpCodes.Ret);

      // GetHashCode()
      ilgeneratorGetHashCode.Emit(OpCodes.Stloc_0);
      ilgeneratorGetHashCode.Emit(OpCodes.Ldloc_0);
      ilgeneratorGetHashCode.Emit(OpCodes.Ret);

      // ToString()
      ilgeneratorToString.Emit(OpCodes.Ldloc_0);
      ilgeneratorToString.Emit(OpCodes.Ldstr, properties.Count == 0 ? "{ }" : " }");
      ilgeneratorToString.Emit(OpCodes.Callvirt, StringBuilderAppendString);
      ilgeneratorToString.Emit(OpCodes.Pop);
      ilgeneratorToString.Emit(OpCodes.Ldloc_0);
      ilgeneratorToString.Emit(OpCodes.Callvirt, ToStringMethod);
      ilgeneratorToString.Emit(OpCodes.Ret);

      type= tb.CreateTypeInfo().AsType();
      if (type.IsGenericTypeDefinition)
        type= type.MakeGenericType(propDefs.Select(p => p.Type).ToArray());

      return type;
    }

    // We recreate this by combining all property names and types, separated by a "|".
    private static string generateTypeKey(IEnumerable<DynamicProperty> props) => string.Join("|", props.Select(p => encodeName(p.Name) + "~" + p.Type.FullName).ToArray());

    // We escape the \ with \\, so that we can safely escape the "|" (that we use as a separator) with "\|"
    private static string encodeName(string name) => name.Replace(@"\", @"\\").Replace(@"|", @"\|");
  }
}
