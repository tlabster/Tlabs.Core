using System;
using System.Collections.Generic;
using System.Linq;

namespace Tlabs.Dynamic {


  /// <summary>Exception thrown on expression syntax error.</summary>
  public class ExpressionSyntaxException : AppConfigException {
    /// <summary>Syntax errors.</summary>
    public readonly IList<ExpressionSyntaxException>? SyntaxErrors;

    /// <summary>Default ctor</summary>
    public ExpressionSyntaxException() : base() { }

    /// <summary>Ctor from message</summary>
    public ExpressionSyntaxException(string message) : base(message) { }

    /// <summary>Ctor from message and inner exception.</summary>
    public ExpressionSyntaxException(string message, Exception e) : base(message, e) { }

    /// <summary>Ctor from message and inner exception.</summary>
    public ExpressionSyntaxException(IList<ExpressionSyntaxException> syntaxErrors)
    : base($"Compilation failed after detection of {syntaxErrors.Count} expression syntax error(s)." + allErrors(syntaxErrors))
    {
      this.SyntaxErrors= syntaxErrors;
    }

    private static string allErrors(IList<ExpressionSyntaxException> syntaxErrors) {
      return   null != syntaxErrors
             ? "\n" + string.Join("  \n", syntaxErrors.Select(err => err.Message))
             : "";
    }
  }

}
