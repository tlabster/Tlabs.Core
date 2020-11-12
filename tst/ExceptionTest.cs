using System;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Tests {
  public class ExceptionTest {
    ITestOutputHelper tstout;
    public ExceptionTest(ITestOutputHelper tstout) {
      this.tstout= tstout;
    }
    internal static readonly Regex TMPL_PATTERN= new Regex(
      @"{(\w+)}",
      RegexOptions.Singleline
    );

    const string msgTmpl= "With '{par01}' and {par02} resolved.";

    [Fact]
    public void ResolvedMsgParamsTest() {
      var resolved= ExceptionDataKey.ResolvedMsgParams(msgTmpl, out var data, 1, "second");
      Assert.Equal("With '1' and second resolved.", resolved);
      Assert.Equal(msgTmpl, data[ExceptionDataKey.MSG_TMPL]);
      Assert.Equal(1, data[(ExceptionDataKey)"par01"]);
      Assert.Equal("second", data[(ExceptionDataKey)"par02"]);
    }

    [Fact]
    public void ExcNewTest() {
      try {
       throw EX.New<InvalidOperationException>(msgTmpl, 1, "second");
      }
      catch (InvalidOperationException exc) {
        Assert.Equal("With '1' and second resolved.", exc.Message);
        Assert.Equal(msgTmpl, exc.MsgTemplate());
        Assert.Equal(1, exc.Data[(ExceptionDataKey)"par01"]);
        Assert.Equal("second", exc.Data[(ExceptionDataKey)"par02"]);
        var msgData= exc.TemplateData();
        Assert.False(msgData.ContainsKey(ExceptionDataKey.MSG_TMPL.Key));
        Assert.Equal(2, msgData.Count);
        Assert.NotEmpty(exc.Source);
        tstout.WriteLine("Exception.Source: {0}", exc.Source);
        exc.Source= "my-src";
        Assert.Equal("my-src", exc.Source);
      }
    }

    [Fact]
    public void ResolvedMsgTest() {
      var plainMsg= "plain msg";
      Assert.Equal(plainMsg, new Exception(plainMsg).ResolvedMsgTemplate());
      Assert.Equal("With 'one' and 2 resolved.", new Exception(plainMsg).SetTemplateData(msgTmpl, "one", 2).ResolvedMsgTemplate());
    }
  }
}
