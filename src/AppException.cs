using System;
using Microsoft.Extensions.Logging;
namespace Tlabs {

  ///<summary>Helper <see cref="Exception"/> extension to log exceptions within <c>catch() when ( )</c> clauses.</summary>
  public static class LogExceptionExt {

    ///<summary>Log error and return false.</summary>
    public static bool LogError(this Exception ex, ILogger log, string msg, params object[] args) {
#pragma warning disable CA2254    //non const msg template needed here
      log.LogError(0, ex, msg, args);
      return false;
    }

    ///<summary>Log warning (w/o stack trace) and return false.</summary>
    public static bool LogWarn(this Exception ex, ILogger log, string msg, params object[] args) {
#pragma warning disable CA2254    //non const msg template needed here
      log.LogWarning($"{msg} - ({ex.Message})", args);
      return false;
    }

    ///<summary>Log debug diagnostics and return false.</summary>
    public static bool LogDiagnostics(this Exception ex, string msg, params object[] args) {
      System.Diagnostics.Debug.WriteLine($"{msg} - ({ex.Message})", args);
      return false;
    }
  }

  /// <summary>Application base exception.</summary>
  public class GeneralException : Exception {

    /// <summary>Default ctor</summary>
    public GeneralException() : base() { }

    /// <summary>Ctor from message</summary>
    public GeneralException(string message) : base(message) { }

    /// <summary>Ctor from message and inner exception.</summary>
    public GeneralException(string message, Exception e) : base(message, e) { }

  }

  /// <summary>Application configuration exception.</summary>
  public class AppConfigException: GeneralException {

    /// <summary>Default ctor</summary>
    public AppConfigException() : base() { }

    /// <summary>Ctor from message</summary>
    public AppConfigException(string message) : base(message) { }

    /// <summary>Ctor from message and inner exception.</summary>
    public AppConfigException(string message, Exception e) : base(message, e) { }

  }

}