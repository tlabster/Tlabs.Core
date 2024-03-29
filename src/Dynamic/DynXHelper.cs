﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;

namespace Tlabs.Dynamic {

  ///<summary>Dynamic expression helper</summary>
  public static class DynXHelper {

    ///<summary>Converts <paramref name="methodInfo"/> into a delegeate</summary>
    public static Delegate AsDelegate(this MethodInfo methodInfo, object? target= null) {
      var parmTypes= methodInfo.GetParameters().Select(parm => parm.ParameterType);
      var delegateType= Expression.GetDelegateType(parmTypes.Append(methodInfo.ReturnType).ToArray());

      if (methodInfo.IsStatic)
        return methodInfo.CreateDelegate(delegateType);
      return methodInfo.CreateDelegate(delegateType, target);
    }


    ///<summary>Compiled context expression info.</summary>
    public struct ContextExpression {
      ///<summary>Context expression</summary>
      public InvocationExpression Expression;
      ///<summary>Resolved context object properties</summary>
      public IEnumerable<Expression> ResolvedCtxParams;
    }


    ///<summary>
    /// <se cref="LambdaExpression"/> compiled from <paramref name="code"/> with access to the public properties of <typeparamref name="TCtx"/> (and <paramref name="funcLib"/>) returning <typeparamref name="TRet"/>.
    ///</summary>
    public static Expression<Func<TCtx, TRet>> BuildLambdaExpression<TCtx, TRet>(string code, IReadOnlyDictionary<string, object> funcLib, IReadOnlyDictionary<string, Type> ctxConverter) {
      var ctxObj= Expression.Parameter(typeof(TCtx));
      var ctxExprInfo= BuildExpression(code, ctxObj, typeof(TRet), funcLib, ctxConverter);
      return Expression.Lambda<Func<TCtx, TRet>>(ctxExprInfo.Expression, ctxObj);
    }


    ///<summary>
    /// Parse the <paramref name="expression"/> with access to the public properties of <paramref name="ctxType"/> (and <paramref name="funcLib"/>) returning <paramref name="retType"/>
    /// into a <see cref="Expression"/> that takes <paramref name="ctxType"/> (the context object) as parameter.
    ///</summary>
    ///<remarks>
    /// In case any of the <paramref name="ctxType"/>'s properties should give an object of a dynamically created type
    /// (like with <see cref="DynamicClassFactory.CreateType(IList{DynamicProperty}, Type, string)"/>):
    /// <list>
    /// <item><term>In <paramref name="ctxType"/> define these properties with type <see cref="object"/>.
    /// </term></item>
    /// <item><term>Provide a <paramref name="ctxConverter"/> dictionary with the name and the actual dynamic <see cref="Type"/> of the property.
    /// </term></item>
    /// </list>
    /// The property exposed to the expression scope will now get the actual dynamic type given by the <paramref name="ctxConverter"/>.
    /// <para>
    /// </para>
    ///</remarks>
    public static ContextExpression BuildExpression(string expression, ParameterExpression ctxType, Type retType, IReadOnlyDictionary<string, object> funcLib, IReadOnlyDictionary<string, Type> ctxConverter) {
      //var ctxProps= ctxType.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)/*.Where(p => p.CanRead)*/.ToList();
      var ctxProps= ctxType.Type.getFlattenedProperties();
      var exprParams= ctxProps.Select(p => {
        if (!ctxConverter.TryGetValue(p.Name, out var paramType))
          paramType= p.PropertyType;
        return Expression.Parameter(paramType, p.Name);
      });
      var parsedExpr= ParsedExpression(expression, exprParams, retType, funcLib);
      var ctxParams= ctxProps.Select(p => {
        Expression paramVal= Expression.MakeMemberAccess(ctxType, p);
        if (ctxConverter.TryGetValue(p.Name, out var convertionType))
          paramVal= Expression.Convert(paramVal, convertionType);   //perform a type convertion
        return paramVal;
      });

      return new ContextExpression {
        Expression= Expression.Invoke(parsedExpr, ctxParams),
        ResolvedCtxParams= ctxParams
      };
    }

    static IEnumerable<PropertyInfo> getFlattenedProperties(this Type type) {
      if (!type.IsInterface)
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

      return (new Type[] { type }).Concat(type.GetInterfaces())
                                  .SelectMany(i => i.GetProperties());
    }

    ///<summary>
    /// Parse the <paramref name="expression"/> with access to the public properties of <typeparamref name="TCtx"/> (and <paramref name="funcLib"/>) returning <paramref name="retType"/>
    /// into a <see cref="LambdaExpression"/>.
    ///</summary>
    public static LambdaExpression ParsedExpression<TCtx>(string expression, Type retType, IReadOnlyDictionary<string, object> funcLib) {
      ArgumentNullException.ThrowIfNull(expression);
      try {
        return DynamicExpressionParser.ParseLambda(false, typeof(TCtx), retType, expression, funcLib);
      }
      catch (System.Linq.Dynamic.Core.Exceptions.ParseException e) {
        throw EX.New<ExpressionSyntaxException>(EXP_ERR, e.Position, e.Message);
      }
    }


    ///<summary>
    /// Parse the <paramref name="expression"/> with access to <paramref name="exprParams"/> (and <paramref name="funcLib"/>) returning <paramref name="retType"/>
    /// into a <see cref="LambdaExpression"/>.
    ///</summary>
    public static LambdaExpression ParsedExpression(string expression, IEnumerable<ParameterExpression> exprParams, Type retType, IReadOnlyDictionary<string, object> funcLib) {
      try {
        return DynamicExpressionParser.ParseLambda(false, exprParams.ToArray(), retType, expression, funcLib);
      }
      catch (System.Linq.Dynamic.Core.Exceptions.ParseException e) {
        throw EX.New<ExpressionSyntaxException>(EXP_ERR, e.Position, e.Message);
      }
    }

    const string EXP_ERR= "Expression syntax error @{pos} ({syntax})";
  }

}