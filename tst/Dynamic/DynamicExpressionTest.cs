using System;
using System.Collections.Generic;

using Xunit;

namespace Tlabs.Dynamic.Test {

  public class DynamicExpressionTest {
    public class SalesTransaction {
      public string TxId { get; set; }
    }
    public class Member {
      public string Name {get; set; }
    }

    public interface IBaseContext {
      SalesTransaction Sales { get; set; }
    }

    public interface IExpressionContext : IBaseContext {
      XYZ__ProductBody Product { get; set; }
    }

    interface IConvertedExpressionContext : IBaseContext {
      object Product { get; set; }
    }

    public class ExpressionContext : IExpressionContext {
      public XYZ__ProductBody Product { get; set; }
      public SalesTransaction Sales { get; set; }
      public Member Member { get; set; }
    }

    class ConvertedExpressionContext : IConvertedExpressionContext {
      public object Product { get; set; }
      public SalesTransaction Sales { get; set; }
      public Member Member { get; set; }
    }

    public class XYZ__ProductBody {
      public XYZ__ProductBody() {
        ProdNumber= "X0009871";
        Properties= new Dictionary<string, object> {
          ["prop01"]= "test text",
          ["prop02"]= new decimal?(123.45M)
        };
      }
      public string ProdNumber { get; set; }
      public string Name { get; set; }
      public string HeadCatagory { get; set; }
      public string Category { get; set; }
      public string SubCategory { get; set; }
      public IDictionary<string, object> Properties { get; set; }
    }

    static readonly ExpressionContext TYPED_CTX= new ExpressionContext {
      Sales= new SalesTransaction {
        TxId= "TX__00001"
      },
      Product= new XYZ__ProductBody()
    };
    static readonly ConvertedExpressionContext UNTYPED_CTX= new ConvertedExpressionContext {
      Sales= new SalesTransaction {
        TxId= "TX__00001"
      },
      Product= new XYZ__ProductBody()
    };


    [Fact]
    public void DynExprTest() {
      var dynExpr= new DynamicExpression<IExpressionContext, bool>("Product != null && Product.ProdNumber.StartsWith(\"X\") && Sales.TxId.StartsWith(\"TX__\")");
      Assert.NotNull(dynExpr);
      Assert.True(dynExpr.Evaluate(TYPED_CTX));


      dynExpr= new DynamicExpression<IExpressionContext, bool>("it.Product != null && Product.ProdNumber.StartsWith(\"X\") && it.Sales.TxId.StartsWith(\"TX__\")");
      Assert.NotNull(dynExpr);
      Assert.True(dynExpr.Evaluate(TYPED_CTX));

      var dynStrExpr= new DynamicExpression<IExpressionContext, string>("it.Product.ProdNumber + \": \" + (DateTime.Now).ToString()");
      Assert.NotNull(dynStrExpr);
      Assert.NotEmpty(dynStrExpr.Evaluate(TYPED_CTX));

      dynExpr= new DynamicExpression<IExpressionContext, bool>(@"
           it.Product != null
        && ""test text"" == Product.Properties[""prop01""].ToString()
        && Product.Properties[""prop01""].Equals(""test text"")");
      Assert.NotNull(dynExpr);
      Assert.True(dynExpr.Evaluate(TYPED_CTX));

      var ctx= new ExpressionContext();
      ctx.Sales= TYPED_CTX.Sales;
      Assert.False(dynExpr.Evaluate(ctx));  //Product == null, check conditional short-circuiting &&
    }

    [Fact]
    public void ConvertedDynRulesTest() {
      var dynExpr= new DynamicExpression<IConvertedExpressionContext, bool>(
        "Product != null && Product.ProdNumber.StartsWith(\"X\") && Sales.TXid.StartsWith(\"TX__\")",
        new Dictionary<string, Type> { ["Product"]= typeof(XYZ__ProductBody) }
      );
      Assert.NotNull(dynExpr);
      Assert.True(dynExpr.Evaluate(UNTYPED_CTX));

      var dynExpr2= new DynamicExpression<ConvertedExpressionContext, bool>(
        "Product != null && Product.ProdNumber.StartsWith(\"X\") && Sales.TXid.StartsWith(\"TX__\")",
        new Dictionary<string, Type> { ["Product"]= typeof(XYZ__ProductBody) }
      );
      Assert.NotNull(dynExpr2);
      Assert.True(dynExpr2.Evaluate(UNTYPED_CTX));

      Assert.Throws<ExpressionSyntaxException>(() => new DynamicExpression<IConvertedExpressionContext, bool>(
        "it.Product != null && Product.ProdNumber.StartsWith(\"X\") && it.Sales.TXid.StartsWith(\"TX__\")",
        new Dictionary<string, Type> { ["Product"]= typeof(XYZ__ProductBody) }
      ));
    }

  }
}