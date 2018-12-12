using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace Tlabs.Dynamic {

  ///<summary>
  /// Objects of this class represent dynamically compiled expressions to be evaluated by
  /// calling the delegate provided by the <see cref="DynamicExpression{TCtx, TRes}.Evaluate">Evaluate</see> property.
  ///</summary>
  ///<typeparam name="TCtx">
  /// <para>The expression's context data type.</para>
  /// <para>Each public property of this type is going to be accessible by its property name in the direct scope of the expression.
  /// An object type like: <code>new { Name= "abc", Value= 123 }</code> would make its properties 'Name' and 'Value' accessible for a expression
  /// <code>"Name != "xyz" and Value > 100"</code>.
  ///</para>
  ///</typeparam>
  ///<typeparam name="TRes">
  /// <para>The expression's result type.</para>
  /// This must be the type that the given expression results to, i.e. a comparision expression like <code>"Value == 123"</code> would have the result type <see cref="bool"/>.
  ///</typeparam>
  public class DynamicExpression<TCtx, TRes> {
    
    /// <summary>Exception thrown on expression syntax error.</summary>
    public class SyntaxException : AppConfigException {
      /// <summary>Syntax errors.</summary>
      public readonly IList<SyntaxException> SyntaxErrors;

      /// <summary>Default ctor</summary>
      public SyntaxException() : base() { }

      /// <summary>Ctor from message</summary>
      public SyntaxException(string message) : base(message) { }

      /// <summary>Ctor from message and inner exception.</summary>
      public SyntaxException(string message, Exception e) : base(message, e) { }

      /// <summary>Ctor from message and inner exception.</summary>
      public SyntaxException(IList<SyntaxException> syntaxErrors) : base($"Compilation failed after detection of {syntaxErrors.Count} expression syntax error(s).") {
        this.SyntaxErrors= syntaxErrors;
      }
    }

    private string expression;
    private Func<TCtx, TRes> exprDelegate;

    ///<summary>Ctor from <paramref name="expression"/> and <paramref name="ctxConverter"/></summary>
    ///<remarks>
    /// In case any of the <typeparamref name="TCtx"/>'s properties should give an object of a dynamically created type
    /// (like with <see cref="DynamicClassFactory.CreateType(IList{DynamicProperty}, Type, string)"/>):
    /// <list>
    /// <item><term>In <typeparamref name="TCtx"/> define these properties with type <see cref="object"/>.
    /// </term></item>
    /// <item><term>In <typeparamref name="TCtx"/>Provide a <paramref name="ctxConverter"/> dictionary with the name and the actual dynamic <see cref="Type"/> of the property.
    /// </term></item>
    /// </list>
    /// The property exposed to the expression scope will now get the actual dynamic type given by the <paramref name="ctxConverter"/>.
    /// <para>
    /// </para>
    ///</remarks>
    public DynamicExpression(string expression, IDictionary<string, Type> ctxConverter = null) {
      if (null == expression) throw new ArgumentNullException(nameof(expression));

      var lamda=   null == ctxConverter
                   ? (Expression<Func<TCtx, TRes>>) parsedExpression(expression)
                   : buildExpression(expression, ctxConverter);

      this.exprDelegate= lamda.Compile();
    }

    /// <summary>Delegate to be called to evaluate the expression.</summary>
    ///<example><code>
    /// var expr = new DynamicExpression&lt;MyContextType, bool&gt;("Value > 100");
    /// bool result = expr.Evaluate(myContextObject);
    ///</code></example>
    public Func<TCtx, TRes> Evaluate => exprDelegate;

    /// <summary>Expression source.</summary>
    public string Source => this.expression;

    private Expression<Func<TCtx, TRes>> buildExpression(string expression, IDictionary<string, Type> ctxConverter) {
      var ctxProps= typeof(TCtx).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead).ToList();
      var exprParams= ctxProps.Select(p => {
        Type paramType;
        if (!ctxConverter.TryGetValue(p.Name, out paramType))
          paramType= p.PropertyType;
        return Expression.Parameter(paramType, p.Name);
      });
      var parsedExpr= parsedExpression(expression, exprParams);
      var ctxObj= Expression.Parameter(typeof(TCtx));
      var ctxParams= ctxProps.Select(p => {
        Expression paramVal= Expression.MakeMemberAccess(ctxObj, p);
        Type convertionType;
        if (ctxConverter.TryGetValue(p.Name, out convertionType))
          paramVal= Expression.Convert(paramVal, convertionType);   //perform a type convertion
        return paramVal;
      });

      return Expression.Lambda<Func<TCtx, TRes>>(Expression.Invoke(parsedExpr, ctxParams), ctxObj);
    }

    private LambdaExpression parsedExpression(string expression) {
      if (null == expression) throw new ArgumentNullException(nameof(expression));
      try {
        return DynamicExpressionParser.ParseLambda(false, typeof(TCtx), typeof(TRes), expression);  //, Formula.Function.Library);
      }
      catch (System.Linq.Dynamic.Core.Exceptions.ParseException e) {
        throw new SyntaxException(expressionError(e));
      }
    }

    private LambdaExpression parsedExpression(string expression, IEnumerable<ParameterExpression> exprParams) {
      try {
        return DynamicExpressionParser.ParseLambda(false, exprParams.ToArray(), typeof(TRes), expression);  //, Formula.Function.Library);
      }
      catch (System.Linq.Dynamic.Core.Exceptions.ParseException e) {
        throw new SyntaxException(expressionError(e));
      }
    }

    private static string expressionError(System.Linq.Dynamic.Core.Exceptions.ParseException e, string msg= "Expression syntax error") {
      return $"{msg} @{e.Position} ({e.Message})";
    }
  }

}