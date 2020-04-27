using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;


namespace Tlabs.Dynamic {

  ///<summary>Class to provide property access to instances of a dynamically provided (typically generated) type.</summary>
  public class DynamicAccessor {
    private readonly Property nilProperty= new Property {
      Get= (o) => null,
      Set= (t, o) => { }
    };
    private Dictionary<string, Property> accessorMap= new Dictionary<string, Property>(StringComparer.OrdinalIgnoreCase);

    ///<summary>Ctor from <paramref name="targetType"/>.</summary>
    public DynamicAccessor(Type targetType) {
      if (null == (TargetType= targetType)) throw new ArgumentNullException(nameof(targetType));
      var coerceMethod= GetType().GetMethod("coerceIntoTargetValue", BindingFlags.Static | BindingFlags.NonPublic);

      foreach (var pi in targetType.GetProperties()) {
        /* Create accessor delegates:
         * Explicitly convert untyped target parameter into bodyType and property type into object
         * to avoid any Delegate.DynamicInvoke()... 
         */
        var targetParam= Expression.Parameter(typeof(object));
        var valParam= Expression.Parameter(typeof(object));
        // var propParam= Expression.Parameter(pi.PropertyType);
        var getterBody= Expression.Convert(Expression.Call(Expression.Convert(targetParam, targetType), pi.GetMethod), typeof(object));
        var setterBody=   null != pi.SetMethod
                        // ? (Expression)Expression.Call(Expression.Convert(targetParam, targetType), pi.SetMethod, Expression.Convert(Expression.Call(cngTypeMethod, valParam, convType), pi.PropertyType))
                        ? (Expression)Expression.Call(Expression.Convert(targetParam, targetType), pi.SetMethod, Expression.Convert(Expression.Call(coerceMethod, valParam, Expression.Constant(pi.PropertyType)), pi.PropertyType))
                        : (Expression)Expression.Empty(); //NoOp if read-only
        accessorMap[pi.Name]= new Property {
          Get= Expression.Lambda<Func<object, object>>(getterBody, targetParam).Compile(),
          Set= Expression.Lambda<Action<object, object>>(setterBody, targetParam, valParam).Compile()
        };
      }

    }

    private static object coerceIntoTargetValue(object val, Type targetType) {
      IEnumerable valEnum;
      if (null == val) return val;
      targetType= Nullable.GetUnderlyingType(targetType) ?? targetType;
      if (targetType.IsAssignableFrom(val.GetType()))
        return val;                                       //no convertion neccessary
      
      var cv= val as IConvertible;
      if (null != cv)                                     // is convertible?
        return Convert.ChangeType(cv, Nullable.GetUnderlyingType(targetType) ?? targetType);   //convert to underlying or targetType

      if (targetType.IsGenericType && null != (valEnum= val as IEnumerable)) {
        /*  Support convertion of types that only implement IEnumerable (like with strange stuff like Newtonsoft.Json.Linq.JArray...))
         *  into a target type implementing IList<>.
        */
        var itemType= targetType.GenericTypeArguments[0];
        Type targetListType= typeof(List<>).MakeGenericType(itemType);
        if (targetType.IsAssignableFrom(targetListType)) {
          IList lst= (IList)Activator.CreateInstance(targetListType);
          foreach (var itm in valEnum)
            lst.Add(Convert.ChangeType(itm, itemType));
          val= lst;
        }
      }
      return val;
    }

    ///<summary>Accessor's target type.</summary>
    public Type TargetType { get; }

    ///<summary>Indexer to return <see cref="Property"/> for <paramref name="name"/>.</summary>
    public Property this[string name] {
      get {
        Property acc;
        return accessorMap.TryGetValue(name, out acc) ? acc : nilProperty;
      }
    }

    ///<summary>Check if <see cref="Property"/> for <paramref name="name"/> exists.</summary>
    public bool Has(string name) => accessorMap.ContainsKey(name);

    ///<summary>Returns a <c>IDictionary</c> to access all properties of <paramref name="target"/>.</summary>
    public IDictionary<string, object> ToDictionary(object target) => new AccessDictionary(this, target);

    ///<summary>Returns a <c>IDictionary</c> to access all property values of <paramref name="target"/>.</summary>
    ///<remarks>Any changes to the values of the returned dictionary will not be reflected in the <paramref name="target"/> object.</remarks>
    public IDictionary<string, object> ToValueDictionary(object target) => new AccessDictionary(this, target).ToDictionary(p => p.Key, p => p.Value); 

    ///<summary>Getter / Setter accessor.</summary>
    public struct Property {
      ///<summary>Getter delegate.</summary>
      public Func<object, object> Get;
      ///<summary>Setter delegate.</summary>
      public Action<object, object> Set;
    }


    private class AccessDictionary : IDictionary<string, object> {
      private DynamicAccessor acc;
      private object obj;

      public AccessDictionary(DynamicAccessor accessor, object target) {
        this.acc= accessor;
        this.obj= target;
        var targetType= target.GetType();
        if (!acc.TargetType.IsAssignableFrom(targetType)) throw new ArgumentException($"Type of target object {targetType} does not match accessor.TargetType: {accessor.TargetType}");
      }

      public object this[string key] {
        get => acc[key].Get(obj);
        set => acc[key].Set(obj, value);
      }

      public ICollection<string> Keys => acc.accessorMap.Keys;

      public ICollection<object> Values => acc.accessorMap.Keys.Select(k => this[k]).ToList();

      public int Count => acc.accessorMap.Count;

      public bool IsReadOnly => false;

      public void Add(string key, object value) {
        throw new NotImplementedException();
      }

      public void Add(KeyValuePair<string, object> item) => throw new InvalidOperationException();

      public void Clear() => throw new InvalidOperationException();

      public bool Contains(KeyValuePair<string, object> item) {
        Property p;
        object v;
        if (acc.accessorMap.TryGetValue(item.Key, out p)) {
          v= p.Get(obj);
          return   null == v
                 ? null == item.Value
                 : v.Equals(item.Value);
        }
        return false;
      }

      public bool ContainsKey(string key) => acc.accessorMap.ContainsKey(key);

      public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
        if (null == array) throw new ArgumentNullException(nameof(array));
        if (arrayIndex + this.Count -1 >= array.Length) throw new ArgumentException(nameof(array));
        foreach (var pair in this) 
          array[arrayIndex++]= pair;
      }

      public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
        foreach (var k in acc.accessorMap.Keys)
          yield return KeyValuePair.Create<string, object>(k, this[k]);
      }

      public bool Remove(string key) => throw new InvalidOperationException();

      public bool Remove(KeyValuePair<string, object> item) => throw new InvalidOperationException();

      public bool TryGetValue(string key, out object value) {
        Property p;
        if (acc.accessorMap.TryGetValue(key, out p)) {
          value= p.Get(obj);
          return true;
        }
        value= null;
        return false;
      }

      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
  }

}