﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Tlabs.Misc {
  using Properties= IDictionary<string, object?>;

  /// <summary>Extension methods for accessing properties of a <see cref="IDictionary{K, T}"/> with K is string and T is object.</summary>
  public static class PropertyExt {
    /// <summary>Return a property's string value or <paramref name="defaultVal"/> if not existing or not a string.</summary>
    public static string? GetString(this Properties prop, string propKey, string? defaultVal) {
      if (prop.TryGetValue(propKey, out var val) && val is string ret) return ret;
      return defaultVal;
    }

    /// <summary>Return a property's string value or null if not existing or not a string.</summary>
    public static string? GetString(this Properties prop, string propKey) {
      return PropertyExt.GetString(prop, propKey, null);
    }

    /// <summary>Return a property's integer value or <paramref name="defaultVal"/> if not existing or not convertible to int.</summary>
    public static int GetInt(this Properties prop, string propKey, int defaultVal) {
      if (prop.TryGetValue(propKey, out var val) && val is IConvertible cv)
        return cv.ToInt32(System.Globalization.NumberFormatInfo.InvariantInfo);
      return defaultVal;
    }

    /// <summary>Return a property's boolean value or <paramref name="defaultVal"/> if not existing or not convertible to bool.</summary>
    public static bool GetBool(this Properties prop, string propKey, bool defaultVal) {
      if (prop.TryGetValue(propKey, out var val) && val is IConvertible cv)
        return cv.ToBoolean(System.Globalization.NumberFormatInfo.InvariantInfo);
      return defaultVal;
    }

    /// <summary>Return a property's value or <paramref name="defaultVal"/> if not existing - in that case is also set as new properties value.</summary>
    public static object? GetOrSet(this Properties prop, string propKey, object? defaultVal) {
      if (!prop.TryGetValue(propKey, out var val))
        prop[propKey]= (val= defaultVal);
      return val;
    }

    /// <summary>Tries to resolve a value from (optionaly) nested properties dictionaries.</summary>
    /// <remarks>
    /// Assuming a <paramref name="propKeyPath"/> of <c>"p1.p2.p3"</c>. This would be (tried)
    /// to be resolved like: <c>properties["p1"]["p2"]["p3"]</c>,
    /// <para>
    /// The first key token of the <paramref name="propKeyPath"/> that is associated with a non dictionary value (or the last)
    /// is returned in <paramref name="resolvedKey"/>
    /// </para>
    /// </remarks>
    /// <param name="prop">(optionaly nested) properties dictionary</param>
    /// <param name="propKeyPath">properties key path (using <paramref name="pathSep"/> as path separator)</param>
    /// <param name="val">resolved value</param>
    /// <param name="resolvedKey">resolved key</param>
    /// <param name="pathSep">Optional path seperator char (defaults to '.')</param>
    /// <returns>true if a value could be resolved using the <paramref name="propKeyPath"/></returns>
    public static bool TryResolveValue(this Properties prop, string propKeyPath, [MaybeNullWhen(false)] out object val, out string? resolvedKey, char pathSep= '.') {
      val= resolvedKey= null;
      var valDict= prop;
      var keyToks= propKeyPath.Split(pathSep);
      int l= 0;
      foreach (string ktok in keyToks) {
        resolvedKey= ktok;
        if (!valDict.TryGetValue(ktok, out val)) return false;
        ++l;
        valDict= val as IDictionary<string, object?>;
        if (null == valDict) {   //no more dictionaries to resolve
          if (l == keyToks.Length) break; //resolved
          return false;
        }
      }
      return null != val;
    }

    /// <summary>Resolved property value.</summary>
    /// <param name="prop">A properties dictionary</param>
    /// <param name="propSpecifier">A property spcifier. If it is a name or property path enclosed in brackets like
    /// '[name.subKey]', the contents of the bracket are tried to be resolved with
    /// <see cref="TryResolveValue(IDictionary{string, object}, string , out object, out string, char)"/></param>
    /// <returns>The resolved property value given by the <paramref name="propSpecifier"/> or if could not be resolved as a property, the
    /// <paramref name="propSpecifier"/> it self.</returns>
    public static object ResolvedProperty(this Properties prop, string propSpecifier) {
      object propVal= propSpecifier;  //default return
      if (null == propSpecifier
          || propSpecifier.Length < 3
          || '[' != propSpecifier[0]
          || ']' != propSpecifier[propSpecifier.Length-1]) return propVal;
      if (TryResolveValue(prop, propSpecifier.Substring(1, propSpecifier.Length-2), out var o, out _))
        propVal= o;

      return propVal;
    }

    /// <summary>Set <paramref name="val"/> to resolved (optionaly) nested properties dictionary.</summary>
    /// <remarks>
    /// Assuming a <paramref name="propKeyPath"/> of <c>"p1.p2.p3"</c>. This would be (tried)
    /// to be resolved like: <c>properties["p1"]["p2"]["p3"] = val</c>,
    /// <para>
    /// Every but the last tokens of the <paramref name="propKeyPath"/> that could not be resolved into a dictionary value
    /// gets created as a new dictionary, if not already existing. If it exists, but is not of dictionary type - false is returned.
    /// </para>
    /// </remarks>
    /// <param name="prop">(optionaly nested) properties dictionary</param>
    /// <param name="propKeyPath">properties key path (using '.' as path delimiter)</param>
    /// <param name="val">value to be set</param>
    /// <param name="resolvedKey">resolved key (last token of the path on success)</param>
    /// <param name="pathSep">Optional path seperator char (defaults to '.')</param>
    /// <returns>true if  value could be set</returns>
    public static bool SetResolvedValue(this Properties prop, string propKeyPath, object? val, out string? resolvedKey, char pathSep= '.') {
      resolvedKey= null;
      var valDict= prop;
      var keyToks= propKeyPath.Split(pathSep);
      Properties? dict;
      int l= 0;
      foreach (string ktok in keyToks) {
        if (++l == keyToks.Length) {
          valDict[resolvedKey= ktok]= val;
          return true;
        }
        if (!valDict.TryGetValue((resolvedKey= ktok), out var o)) {
          valDict[ktok]= dict= new Dictionary<string, object?>();
          valDict= dict;
          continue;
        }
        if (null == (dict= o as IDictionary<string, object?>)) return false;
        valDict= dict;
      }
      throw new InvalidOperationException($"Invalid key path: {propKeyPath}");  //must not happen
    }

    /// <summary>Convert <see cref="IReadOnlyDictionary{K, T}"/> into read-only <see cref="IDictionary{K, T}"/>.</summary>
    public static IReadOnlyDictionary<string, object?> ToReadonly(this Properties prop) {
      if (prop is IReadOnlyDictionary<string, object?> rdProp) return rdProp;
      return new Dictionary<string, object?>(prop);
    }

  }

}
