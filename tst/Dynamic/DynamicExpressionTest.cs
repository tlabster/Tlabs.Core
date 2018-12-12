using System;
using System.Collections.Generic;

using Xunit;

namespace Tlabs.Dynamic.Test {

  public class DynamicExpressionTest {
    class SalesTransaction {
      public string TxId { get; set; }
    }
    class Member {
      public string Name {get; set; }
    }
    class BaseExpressionContext {
      public SalesTransaction Sales { get; set; }
      public Member Member { get; set; }
    }
    class ExpressionContext : BaseExpressionContext {
      public XYZ__ProductBody Product { get; set; }
    }

    class ConvertedExpressionContext : BaseExpressionContext {
      public object Product { get; set; }
    }

    class XYZ__ProductBody {
      public string ProdNumber { get; set; }
      public string Name { get; set; }
      public string HeadCatagory { get; set; }
      public string Category { get; set; }
      public string SubCategory { get; set; }
    }

    static readonly ExpressionContext TYPED_CTX= new ExpressionContext {
      Sales= new SalesTransaction {
        TxId= "TX__00001"
      },
      Product= new XYZ__ProductBody {
        ProdNumber= "X0009871"
      }
    };
    static readonly ConvertedExpressionContext UNTYPED_CTX= new ConvertedExpressionContext {
      Sales= new SalesTransaction {
        TxId= "TX__00001"
      },
      Product= new XYZ__ProductBody {
        ProdNumber= "X0009871"
      }
    };


    [Fact]
    public void DynExprTest() {
      var dynExpr= new DynamicExpression<ExpressionContext, bool>("Product != null && Product.ProdNumber.StartsWith(\"X\") && Sales.TxId.StartsWith(\"TX__\")");
      Assert.NotNull(dynExpr);
      Assert.True(dynExpr.Evaluate(TYPED_CTX));


      dynExpr= new DynamicExpression<ExpressionContext, bool>("it.Product != null && Product.ProdNumber.StartsWith(\"X\") && it.Sales.TxId.StartsWith(\"TX__\")");
      Assert.NotNull(dynExpr);
      Assert.True(dynExpr.Evaluate(TYPED_CTX));
    }

    [Fact]
    public void ConvertedDynRulesTest() {
      var dynExpr= new DynamicExpression<ConvertedExpressionContext, bool>(
        "Product != null && Product.ProdNumber.StartsWith(\"X\") && Sales.TXid.StartsWith(\"TX__\")",
        new Dictionary<string, Type> { ["Product"]= typeof(XYZ__ProductBody) }
      );
      Assert.NotNull(dynExpr);
      Assert.True(dynExpr.Evaluate(UNTYPED_CTX));

      Assert.Throws<DynamicExpression<ConvertedExpressionContext, bool>.SyntaxException>(() => new DynamicExpression<ConvertedExpressionContext, bool>(
        "it.Product != null && Product.ProdNumber.StartsWith(\"X\") && it.Sales.TXid.StartsWith(\"TX__\")",
        new Dictionary<string, Type> { ["Product"]= typeof(XYZ__ProductBody) }
      ));
    }

  }
}