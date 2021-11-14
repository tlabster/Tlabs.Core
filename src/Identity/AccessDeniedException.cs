using System;

namespace Tlabs.Identity {
  /// <summary>Exception type that is thrown on denial of access. </summary>
  public class AccessDeniedException : Tlabs.GeneralException {
    /// <summary>Message template. </summary>
    public const string TMPL_MSG= "Access to {entity} from user '{user}' denied.";

    /// <inheritdoc/>
    public AccessDeniedException() : base() { }
    /// <inheritdoc/>
    public AccessDeniedException(string message) : base(message) { }
    /// <inheritdoc/>
    public AccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
  }
}