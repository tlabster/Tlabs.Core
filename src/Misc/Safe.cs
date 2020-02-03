using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Tlabs.Misc {

  /// <summary>Miscelaneous helpers to enhance reliability of resource allocation, error handling, etc..</summary>
  public static class Safe {

    /// <summary>Safely returns a newly allocated object that depends on a <see cref="IDisposable"/> object.</summary>
    /// <remarks>
    /// When the allocation succeeds, the <see cref="IDisposable"/> object of type <typeparamref name="D"/> is assumed to be owned by
    /// the returned object of type <typeparamref name="T"/>. (i.e. the returned object is responsible for disposing)
    /// </remarks>
    /// <typeparam name="T">The type returned.</typeparam>
    /// <typeparam name="D">A a reference type that implements <see cref="IDisposable"/> and has a default ctor.</typeparam>
    /// <param name="allocT">A delegate that creates an instance of type T.</param>
    /// <returns>instance of T</returns>
    public static T Allocated<T, D>(Func<D, T> allocT) where D : class, IDisposable, new() {
      return Safe.Allocated(
        () => new D(),
        (d) => allocT(d)
      );
    }

    /// <summary>Safely returns a newly allocated object that depends on a <see cref="IDisposable"/> object.</summary>
    /// <remarks>
    /// When the allocation succeeds, the <see cref="IDisposable"/> object of type <typeparamref name="D"/> is assumed to be owned by
    /// the returned object of type <typeparamref name="T"/>. (i.e. the returned object is responsible for disposing)
    /// </remarks>
    /// <typeparam name="T">The type returned.</typeparam>
    /// <typeparam name="D">A a reference type that implements <see cref="IDisposable"/>.</typeparam>
    /// <param name="allocD">A delegate that creates an instance of disposable type D</param>
    /// <param name="allocT">A delegate that creates an instance of type T.</param>
    /// <returns>instance of T</returns>
    public static T Allocated<T, D>(Func<D> allocD, Func<D, T> allocT) where D : class, IDisposable {
      D res= null;
      try {
        T ret= allocT(res= allocD());
        res= null;
        return ret;
      }
      finally { if (null != res) res.Dispose(); }
    }

    /// <summary>
    /// Safely returns a newly allocated <see cref="IDisposable"/> object
    /// that depends on another <see cref="IDisposable"/> object.
    /// </summary>
    /// <remarks>
    /// The object is returned after all these operations succeed:<br/>
    /// - creation of the other object<br/>
    /// - creation of the initial object to be returned<br/>
    /// - the further setup of the object to be returned<br/>
    /// If any of these operations fails (by throwing an exception), it is guaranteed that all created objects
    /// get disposed properly.
    /// </remarks>
    /// <typeparam name="T">The type returned.</typeparam>
    /// <typeparam name="D">A a reference type that implements IDisposable and has a default ctor.</typeparam>
    /// <param name="allocD">A delegate that creates an instance of disposable type D</param>
    /// <param name="allocT">A delegate that creates an instance of type T.</param>
    /// <param name="setupT">A delegate to further setup an instance of type T.</param>
    /// <returns>instance of T</returns>
    public static T Allocated<T, D>(Func<D> allocD,
                                    Func<D, T> allocT,
                                    Action<D, T> setupT)
      where T : class, IDisposable
      where D : class, IDisposable {
      D d= null;
      try {
        d= allocD();
        T t= null;
        try {
          t= allocT(d);
          setupT(Undisposable(ref d), t);
          return Undisposable(ref t);
        }
        finally { if (null != t) t.Dispose(); }
      }
      finally { if (null != d) d.Dispose(); }
    }

    /// <summary>
    /// Returns the passed <paramref name="obj"/> and makes the the variable holding this reference undisposable
    /// by setting it to null for the caller.
    /// </summary>
    /// <typeparam name="T">type of <paramref name="obj"/></typeparam>
    /// <param name="obj"><see cref="IDisposable"/></param>
    /// <returns><paramref name="obj"/></returns>
    public static T Undisposable<T>(ref T obj) where T : class, IDisposable {
      T tmp= obj;
      obj= null;
      return tmp;
    }

    ///<summary>Compares <paramref name="location"/> with <paramref name="comparand"/> (of reference type <typeparamref name="T"/> for reference equality and,
    ///if they are equal, replaces <paramref name="location"/> with the value returned from <paramref name="creator"/>.</summary>
    ///<returns>The original value in <paramref name="location"/></returns>
    public static T CompareExchange<T>(ref T location, T comparand, Func<T> creator) where T : class {
      T orgVal;
      if (comparand != (orgVal= location)) return orgVal;
      var newVal= creator();
      if (comparand != (orgVal= Interlocked.CompareExchange<T>(ref location, newVal, comparand)))
        (newVal as IDisposable)?.Dispose();
      return orgVal;
    }

    /// <summary>List of exceptions that are considered disastrous.</summary>
    public static readonly IList<Type> DisastrousExceptions= new ReadOnlyCollection<Type>(new Type[] {
      typeof(OutOfMemoryException),
      typeof(PlatformNotSupportedException),
      typeof(InvalidProgramException)
    });

    /// <summary>Check if an exception type is critical and thus should terminate the process leaving the exception unhandled.</summary>
    /// <remarks>This check should only be used in top level catch clauses.</remarks>
    public static bool NoDisastrousCondition(Exception e) {
      var xtype= e.GetType();
      /* Do not consider derived exceptions !!!
       */
      foreach (var disastrous in DisastrousExceptions)
        if (xtype == disastrous) {
          Environment.FailFast(e.Message, e);
          return false;
        }
      return true;
    }

    /// <summary>Consider whether the <paramref name="e">exception</paramref> is critical and thus must terminate the process fast leaving the exception unhandled.</summary>
    /// <remarks>This check should only be used in top level catch clauses.</remarks>
    public static void HandleDisastrousCondition(Exception e) {
      if (!NoDisastrousCondition(e))
        Environment.FailFast(e.Message, e);
    }

    /// <summary>Loads a type specified as <paramref name="qualifiedTypeName"/>.</summary>
    ///<exception cref="AppConfigException">When type loading failed. Using <paramref name="typeDesc"/> in error...</exception>
    public static Type LoadType(string qualifiedTypeName, string typeDesc) {
      if (string.IsNullOrEmpty(qualifiedTypeName)) throw new ArgumentException(nameof(qualifiedTypeName));

      var typeNameParts= qualifiedTypeName.Split('&');  //split type parameters by '&'

      try {
        if (1 == typeNameParts.Length)  //simple non generic type?
          return Type.GetType(qualifiedTypeName= typeNameParts[0].Trim(), true);
        /* Load generic type with parameters:
         */
        var types= new Type[typeNameParts.Length];
        for (int l = 0; l < types.Length; ++l)
          types[l]= Type.GetType(qualifiedTypeName= typeNameParts[l].Trim(), true);
        try {
          return types[0].MakeGenericType(types.Skip(1).ToArray());
        }
        catch (ArgumentNullException e) {
          throw new AppConfigException($"Invalid generic type parameters in {typeDesc} for generic type: {typeNameParts[0]}", e);
        }
      }
      // Detailed exception mapping:
      catch (ArgumentException argX) { throw new AppConfigException($"Invalid type name {qualifiedTypeName} ({typeDesc}).", argX); }
      catch (TypeLoadException tlX) { throw new AppConfigException($"Type not found {qualifiedTypeName} ({typeDesc}).", tlX); }
      catch (System.IO.FileNotFoundException fnfX) { throw new AppConfigException($"Assembly (or dependend assembly) not found for {qualifiedTypeName} ({typeDesc}).", fnfX); }
      catch (Exception eX) { throw new AppConfigException($"Failed to load {typeDesc}.", eX); }
    }

  }
}
