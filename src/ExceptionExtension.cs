using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tlabs {

  ///<summary>Helper <see cref="Exception"/> extension to manage message template data.</summary>
  public static class EX {

    ///<summary>New exception <typeparamref name="T"/> with template data.</summary>
    public static T New<T>(string msgTemplate, params object[] args) where T : System.Exception {
      var msg= ExceptionDataKey.ResolvedMsgParams(msgTemplate, out var data, args);
      return ((T)Activator.CreateInstance(typeof(T), new object[] { msg })).SetMsgData(data);
    }

    ///<summary>New exception <typeparamref name="T"/> from <paramref name="innerException"/> and template data.</summary>
    public static T New<T>(System.Exception innerException, string msgTemplate, params object[] args) where T : System.Exception {
      var msg= ExceptionDataKey.ResolvedMsgParams(msgTemplate, out var data, args);
      return ((T)Activator.CreateInstance(typeof(T), new object[] { msg, innerException })).SetMsgData(data);
    }

    internal static readonly Regex TMPL_PATTERN= new Regex(
      @"{(\w+)}",
      RegexOptions.Singleline
    );

    ///<summary>Set message data.</summary>
    public static T SetMsgData<T>(this T ex, string key, object data) where T : System.Exception {
      ex.Data[(ExceptionDataKey)key]= data;
      return ex;
    }

    ///<summary>Set message data.</summary>
      public static T SetMsgData<T>(this T ex, IDictionary data) where T : System.Exception {
      var xdata= ex.Data;
      foreach (DictionaryEntry pair in data)
        if (pair.Key is ExceptionDataKey) xdata[pair.Key]= pair.Value;
      return ex;
    }

    ///<summary>Set template (message) data.</summary>
    public static T SetTemplateData<T>(this T ex, string msgTemplate, params object[] args) where T : System.Exception {
      if (null != ex.MsgTemplate()) return ex; //has template data set already
      ExceptionDataKey.ResolvedMsgParams(msgTemplate, out var data, args);
      return ex.SetMsgData(data);
    }

    ///<summary>Set missing template (message) data.</summary>
    public static T SetMissingTemplateData<T>(this T ex, string msgTemplate, params object[] args) where T : System.Exception {
      if (null != ex.MsgTemplate()) return ex; //has template data set already
      return ex.SetTemplateData(msgTemplate, args);
    }

    ///<summary>Resolved message template.</summary>
    public static string ResolvedMsgTemplate<T>(this T ex) where T : System.Exception {
      var tmpl= ex.MsgTemplate();
      return ExceptionDataKey.ResolvedMsgTemplate(tmpl, ex.Data) ?? ex.Message;
    }

    ///<summary>Returns this excption's template message or null if not present.</summary>
    public static string MsgTemplate<T>(this T ex) where T : System.Exception => ex.Data[ExceptionDataKey.MSG_TMPL] as string;

    ///<summary>Template data or null.</summary>
    public static IDictionary<string, object> TemplateData<T>(this T ex) where T : System.Exception {
      if (null == ex.MsgTemplate()) return null;
      var data= new Dictionary<string, object>(ex.Data.Count);
      foreach (DictionaryEntry pair in ex.Data)
        if (pair.Key is ExceptionDataKey k && k != ExceptionDataKey.MSG_TMPL) data[k.Key]= pair.Value;
      return data;
    }
  }

  /// <summary><see cref="System.Exception.Data"/> key type.</summary>
  public class ExceptionDataKey {
    /// <summary>Message template key.</summary>
    public readonly static ExceptionDataKey MSG_TMPL= new ExceptionDataKey(">msgTmpl");

    /// <summary>Explicit convertion from string.</summary>
    public static explicit operator ExceptionDataKey(string key) => new ExceptionDataKey(key);

    /// <summary>Ctor from <paramref name="key"/>.</summary>
    public ExceptionDataKey(string key) {
      if (null == (this.Key= key)) new ArgumentNullException(nameof(key)).SetTemplateData($"{nameof(ExceptionDataKey)} must not be constructed from {{key}}.", null);
    }

    /// <summary>Key value.</summary>
    public string Key { get; }

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is ExceptionDataKey key && Key==key.Key;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Key);

    /// <inheritdoc/>
    public override string ToString() => Key;

    internal static readonly Regex TMPL_PATTERN= new Regex(
      @"{(\w+)}",
      RegexOptions.Singleline
    );
    /// <summary>Resolves the <paramref name="msgTmpl"/> parameters into <paramref name="data"/> using <paramref name="param"/>(s) and return the resulting message.</summary>
    public static string ResolvedMsgParams(string msgTmpl, out IDictionary data, params object[] param) {
      var dataDict= data= new System.Collections.Specialized.ListDictionary();
      data[MSG_TMPL]= msgTmpl;
      return resolveMsg(msgTmpl, (idx, pname) => (dataDict[pname]= param[idx]).ToString());
    }

    /// <summary>Resolves the <paramref name="msgTmpl"/> with <paramref name="data"/>.</summary>
    public static string ResolvedMsgTemplate(string msgTmpl, IDictionary data) => resolveMsg(msgTmpl, (idx, pname) => data[pname]?.ToString() ?? "null");

    static string resolveMsg(string msgTmpl, Func<int, ExceptionDataKey, string> paramVal) {
      int idx= 0;
      if (null == msgTmpl) return null;
      var resolved= TMPL_PATTERN.Replace(msgTmpl, match => {
        var parName= (ExceptionDataKey)match.Groups[1].Value;
        return paramVal(idx++, parName);
      });
      return resolved;
    }

  } //class ExceptionDataKey

}