using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace Tlabs.Dynamic {
  /// <summary>
  /// A factory to create dynamic classes, based on <see href="http://stackoverflow.com/questions/29413942/c-sharp-anonymous-object-with-properties-from-dictionary" />.
  /// </summary>
  public static class DynamicClassFactory {
    class Log { }
    static readonly ILogger<DynamicClassFactory.Log> log= App.Logger<DynamicClassFactory.Log>();

    // EmptyTypes is used to indicate that we are looking for someting without any parameters.
    static readonly Type[] EmptyTypes= Array.Empty<Type>();

    static readonly Tlabs.Misc.BasicCache<string, Type> typeCache= new();

    static readonly ModuleBuilder ModuleBuilder;

    // Some objects we cache
    static readonly CustomAttributeBuilder CompilerGeneratedAttrib= new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(EmptyTypes), Array.Empty<object>());
    static readonly CustomAttributeBuilder DebuggerBrowsableAttrib= new CustomAttributeBuilder(typeof(DebuggerBrowsableAttribute).GetConstructor(new[] { typeof(DebuggerBrowsableState) }), new object[] { DebuggerBrowsableState.Never });
    static readonly CustomAttributeBuilder DebuggerHiddenAttrib= new CustomAttributeBuilder(typeof(DebuggerHiddenAttribute).GetConstructor(EmptyTypes), Array.Empty<object>());

    static readonly ConstructorInfo DefaultCtor= typeof(object).GetConstructor(EmptyTypes);
    static readonly MethodInfo ToStringMethod= typeof(object).GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public);

    static readonly ConstructorInfo StringBuilderCtor= typeof(StringBuilder).GetConstructor(EmptyTypes);
    static readonly MethodInfo StringBuilderAppendString= typeof(StringBuilder).GetMethod("Append", new[] { typeof(string) });
    static readonly MethodInfo StringBuilderAppendObject= typeof(StringBuilder).GetMethod("Append", new[] { typeof(object) });

    static readonly Type EqualityComparer= typeof(EqualityComparer<>);
    static readonly Type EqualityComparerGenericArgument= EqualityComparer.GetGenericArguments()[0];

    static readonly MethodInfo EqualityComparerDefault= EqualityComparer.GetMethod("get_Default", BindingFlags.Static | BindingFlags.Public);
    static readonly MethodInfo EqualityComparerEquals= EqualityComparer.GetMethod("Equals", new[] { EqualityComparerGenericArgument, EqualityComparerGenericArgument });
    static readonly MethodInfo EqualityComparerGetHashCode= EqualityComparer.GetMethod("GetHashCode", new[] { EqualityComparerGenericArgument });

    static readonly Type DefaultBaseType= typeof(object);
    static readonly string typeNamePrefix= "<>__" + nameof(DynamicClassFactory) + "__";
    static int sequence;  //= 0;
    class PropertyDef {
      public string Name;
      public Type Type;
      public IList<DynamicAttribute> Attributes;
      public FieldBuilder Field;
      public override string ToString() => $"{Name}[{Type.Name}]";
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

      parentType??= DefaultBaseType;
      string typeKey=   string.IsNullOrEmpty(typeName)
                      ? generateTypeKey(properties, parentType)
                      : typeName + "~" + parentType.Name;

      Type createNew() => createNewType(properties, parentType, typeName);
      if (log.IsEnabled(LogLevel.Trace)) {
        var type= typeCache[typeKey];
        if (null != type) {
          log.LogTrace("Skipping creation of existing type with key: {k}", typeKey);
          log.LogTrace("Returning existing type: {tp}", type.Name);
        }
      }
      return typeCache[typeKey, createNew];
    }

    private static Type createNewType(IList<DynamicProperty> properties, Type parentType, string typeName) {
      Type type;
      string seq= $"_{Interlocked.Increment(ref sequence)}";
      var genericType= string.IsNullOrEmpty(typeName);

      string name=   typeNamePrefix
                   + (genericType ? $"{seq}`{properties.Count}" : (typeName + seq));
      log.LogTrace("Creating new dynamic type: '{type}'", name);

      TypeBuilder tb= ModuleBuilder.DefineType(name,
                                                 TypeAttributes.AnsiClass
                                               | TypeAttributes.Public
                                               | TypeAttributes.Class
                                               | TypeAttributes.AutoLayout
                                               | TypeAttributes.BeforeFieldInit,
                                               parentType);
      tb.SetCustomAttribute(CompilerGeneratedAttrib);


      var propDefs= properties.Select(prop => new PropertyDef { Name= prop.Name, Type= prop.Type, Attributes= prop.Attributes }).ToList();
      if (log.IsEnabled(LogLevel.Trace)) log.LogTrace("Properties of '{type}': {props}", name, string.Join(", ", propDefs));

      if (genericType) {
        var genericParams=   propDefs.Count > 0
                           ? tb.DefineGenericParameters(propDefs.Select(prop => $"TPar__<{prop.Name}>").ToArray())
                           : Array.Empty<GenericTypeParameterBuilder>();
        for (int l= 0; l < genericParams.Length; ++l) {
          var tp= genericParams[l];
          tp.SetCustomAttribute(CompilerGeneratedAttrib);
          propDefs[l].Type= tp; //use generic type parameter type
        }
      }

      generateProperties(tb, propDefs);

      // ToString()
      MethodBuilder toString= tb.DefineMethod("ToString", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, typeof(string), EmptyTypes);
      toString.SetCustomAttribute(DebuggerHiddenAttrib);
      var ilToString= toString.GetILGenerator();
      ilToString.DeclareLocal(typeof(StringBuilder));
      ilToString.Emit(OpCodes.Newobj, StringBuilderCtor);
      ilToString.Emit(OpCodes.Stloc_0);

      // Equals()
      var equals= tb.DefineMethod("Equals", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, typeof(bool), new[] { typeof(object) });
      equals.SetCustomAttribute(DebuggerHiddenAttrib);
      equals.DefineParameter(1, ParameterAttributes.In, "value");

      var ilEquals= equals.GetILGenerator();
      ilEquals.DeclareLocal(tb);
      ilEquals.Emit(OpCodes.Ldarg_1);
      ilEquals.Emit(OpCodes.Isinst, tb);
      ilEquals.Emit(OpCodes.Stloc_0);
      ilEquals.Emit(OpCodes.Ldloc_0);
      Label equalsLabel= ilEquals.DefineLabel();

      // GetHashCode()
      var getHashCode= tb.DefineMethod("GetHashCode", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, CallingConventions.HasThis, typeof(int), EmptyTypes);
      getHashCode.SetCustomAttribute(DebuggerHiddenAttrib);
      ILGenerator ilGetHashCode= getHashCode.GetILGenerator();
      ilGetHashCode.DeclareLocal(typeof(int));

      if (properties.Count == 0) ilGetHashCode.Emit(OpCodes.Ldc_I4_0);
      else {
        // As done by Roslyn
        // Note that initHash can vary, because string.GetHashCode() isn't "stable" for different compilation of the code
        int initHash= 0;
        foreach (var prop in properties)
          initHash= unchecked(initHash * (-1521134295) + prop.Name.GetHashCode());

        // Note that the CSC seems to generate a different seed for every anonymous class
        ilGetHashCode.Emit(OpCodes.Ldc_I4, initHash);
      }

      var first= true;
      foreach (var prop in propDefs) {
        Type equalityComparerT= EqualityComparer.MakeGenericType(prop.Type);

        // Equals()
        MethodInfo equalityComparerTDefault=   genericType
                                              ? TypeBuilder.GetMethod(equalityComparerT, EqualityComparerDefault)
                                              : equalityComparerT.GetMethod("get_Default", BindingFlags.Static | BindingFlags.Public);
        MethodInfo equalityComparerTEquals=   genericType
                                              ? TypeBuilder.GetMethod(equalityComparerT, EqualityComparerEquals)
                                              : equalityComparerT.GetMethod("Equals", new[] { prop.Type, prop.Type });

        // Illegal one-byte branch at position: 9. Requested branch was: 143.
        // So replace OpCodes.Brfalse_S to OpCodes.Brfalse
        ilEquals.Emit(OpCodes.Brfalse, equalsLabel);
        ilEquals.Emit(OpCodes.Call, equalityComparerTDefault);
        ilEquals.Emit(OpCodes.Ldarg_0);
        ilEquals.Emit(OpCodes.Ldfld, prop.Field);
        ilEquals.Emit(OpCodes.Ldloc_0);
        ilEquals.Emit(OpCodes.Ldfld, prop.Field);
        ilEquals.Emit(OpCodes.Callvirt, equalityComparerTEquals);

        // GetHashCode();
        MethodInfo equalityComparerTGetHashCode=   genericType
                                                 ? TypeBuilder.GetMethod(equalityComparerT, EqualityComparerGetHashCode)
                                                 : equalityComparerT.GetMethod("GetHashCode", new[] { prop.Type });
        ilGetHashCode.Emit(OpCodes.Stloc_0);
        ilGetHashCode.Emit(OpCodes.Ldc_I4, -1521134295);
        ilGetHashCode.Emit(OpCodes.Ldloc_0);
        ilGetHashCode.Emit(OpCodes.Mul);
        ilGetHashCode.Emit(OpCodes.Call, equalityComparerTDefault);
        ilGetHashCode.Emit(OpCodes.Ldarg_0);
        ilGetHashCode.Emit(OpCodes.Ldfld, prop.Field);
        ilGetHashCode.Emit(OpCodes.Callvirt, equalityComparerTGetHashCode);
        ilGetHashCode.Emit(OpCodes.Add);

        // ToString();
        ilToString.Emit(OpCodes.Ldloc_0);
        ilToString.Emit(OpCodes.Ldstr, first ? $"{{ {prop.Name}= " : $", {prop.Name}= ");
        ilToString.Emit(OpCodes.Callvirt, StringBuilderAppendString);
        ilToString.Emit(OpCodes.Pop);
        ilToString.Emit(OpCodes.Ldloc_0);
        ilToString.Emit(OpCodes.Ldarg_0);
        ilToString.Emit(OpCodes.Ldfld, prop.Field);
        ilToString.Emit(OpCodes.Box, prop.Type);
        ilToString.Emit(OpCodes.Callvirt, StringBuilderAppendObject);
        ilToString.Emit(OpCodes.Pop);
        first= false;
      }

      // Equals()
      if (properties.Count == 0) {
        ilEquals.Emit(OpCodes.Ldnull);
        ilEquals.Emit(OpCodes.Ceq);
        ilEquals.Emit(OpCodes.Ldc_I4_0);
        ilEquals.Emit(OpCodes.Ceq);
      }
      else {
        ilEquals.Emit(OpCodes.Ret);
        ilEquals.MarkLabel(equalsLabel);
        ilEquals.Emit(OpCodes.Ldc_I4_0);
      }

      ilEquals.Emit(OpCodes.Ret);

      // GetHashCode()
      ilGetHashCode.Emit(OpCodes.Stloc_0);
      ilGetHashCode.Emit(OpCodes.Ldloc_0);
      ilGetHashCode.Emit(OpCodes.Ret);

      // ToString()
      ilToString.Emit(OpCodes.Ldloc_0);
      ilToString.Emit(OpCodes.Ldstr, properties.Count == 0 ? "{ }" : " }");
      ilToString.Emit(OpCodes.Callvirt, StringBuilderAppendString);
      ilToString.Emit(OpCodes.Pop);
      ilToString.Emit(OpCodes.Ldloc_0);
      ilToString.Emit(OpCodes.Callvirt, ToStringMethod);
      ilToString.Emit(OpCodes.Ret);

      type= tb.CreateTypeInfo().AsType();
      if (type.IsGenericTypeDefinition)
        type= type.MakeGenericType(properties.Select(p => p.Type).ToArray());
      if (log.IsEnabled(LogLevel.Trace)) log.LogTrace("New type '{type}' created.", type.FullName);

      return type;
    }

    private static void generateProperties(TypeBuilder tb, IList<PropertyDef> propDefs) {
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
    }

    // We recreate this by combining all property names and types, separated by a "|".
    private static string generateTypeKey(IEnumerable<DynamicProperty> props, Type parentType)
      => $"{string.Join("|", props)}~{parentType.Name}";

  }
}
