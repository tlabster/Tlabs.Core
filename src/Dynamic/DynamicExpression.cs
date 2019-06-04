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

    private string expression;
    private Func<TCtx, TRes> exprDelegate;

    ///<summary>Ctor from <paramref name="expression"/> and <paramref name="ctxConverter"/></summary>
    ///<remarks>
    /// In case any of the <typeparamref name="TCtx"/>'s properties should give an object of a dynamically created type
    /// (like with <see cref="DynamicClassFactory.CreateType(IList{DynamicProperty}, Type, string)"/>):
    /// <list>
    /// <item><term>In <typeparamref name="TCtx"/> define these properties with type <see cref="object"/>.
    /// </term></item>
    /// <item><term>Provide a <paramref name="ctxConverter"/> dictionary with the name and the actual dynamic <see cref="Type"/> of the property.
    /// </term></item>
    /// </list>
    /// The property exposed to the expression scope will now get the actual dynamic type given by the <paramref name="ctxConverter"/>.
    /// <para>
    /// </para>
    ///</remarks>
    public DynamicExpression(string expression, IDictionary<string, Type> ctxConverter= null, IDictionary<string, object> funcLib= null) {
      if (null == (this.expression= expression)) throw new ArgumentNullException(nameof(expression));
      funcLib= funcLib ?? Misc.Function.Library;

      var lamda=   null == ctxConverter
                   ? (Expression<Func<TCtx, TRes>>) DynXHelper.ParsedExpression<TCtx>(expression, typeof(TRes), funcLib)
                   : DynXHelper.BuildLambdaExpression<TCtx, TRes>(expression, funcLib, ctxConverter);

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
  }

}