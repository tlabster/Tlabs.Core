using System;
using System.IO;
using System.Collections.Generic;

using Tlabs.Misc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;
using Moq;

namespace Tlabs.Config.Tests {

  public class LogFormatterTest : IClassFixture<LogFormatterTest.Fixture> {
    public class Fixture {
      public IServiceCollection SvcColl;
      public ILoggingBuilder LogBldr;
      public IConfiguration TstCfg= new Dictionary<string, string> {
        ["options:dumy"]= "xyz"

      }.ToConfiguration();
      public Fixture() {
        var svcCollMock= new Mock<IServiceCollection>();
        // svcCollMock.Setup(c => c.Configure<It.IsAnyType>(It.IsAny<IConfiguration>()));  //<It.IsAnyType>  <ConsoleFormatterOptions>
        this.SvcColl= svcCollMock.Object;

        var logMock= new Mock<ILoggingBuilder>();
        logMock.Setup(l => l.Services).Returns(this.SvcColl);
        // logMock.Setup(l => l.AddSystemdConsole());
        // logMock.Setup(l => l.AddFile(It.IsAny<IConfiguration>()));
        // logMock.Setup(l => l.AddConsole(It.IsAny<Action<ConsoleLoggerOptions>>()));
        // logMock.Setup(l => l.AddConsoleFormatter<CustomStdoutFormatter, CustomStdoutFormatterOptions>());
        LogBldr= logMock.Object;
      }
    }

    class TestScopeProvider : IExternalScopeProvider {
      string[] scopes= new string[] {"scope1", "scope2"};
      public void ForEachScope<TState>(Action<object, TState> callback, TState state) {
        foreach (var scope in scopes) callback(scope, state);
      }
      public IDisposable Push(object state) => throw new NotImplementedException();
    }
    static TestScopeProvider tstScopeProv= new();

    class FormOpt : IOptions<CustomStdoutFormatterOptions> {
      CustomStdoutFormatterOptions opt;
      public FormOpt(CustomStdoutFormatterOptions opt) => this.opt= opt;
      public CustomStdoutFormatterOptions Value => opt;
    }

    private readonly ITestOutputHelper tstout;
    private readonly LogFormatterTest.Fixture fix;

    public LogFormatterTest(LogFormatterTest.Fixture fix, ITestOutputHelper output) {
      this.fix= fix;
      this.tstout= output;
    }

    [Fact]
    public void FormatterConfigTest() {
      new SysLoggingConfigurator().AddTo(fix.LogBldr, Singleton<Tlabs.Config.Empty>.Instance);
      new FileLoggingConfigurator().AddTo(fix.LogBldr, fix.TstCfg);
      new StdoutLoggingConfigurator().AddTo(fix.LogBldr, Singleton<Tlabs.Config.Empty>.Instance);
    }


    [Fact]
    public void FormatterTest() {
      var opt= new FormOpt(new CustomStdoutFormatterOptions {
        IncludeScopes= true
      });
      var formatter= new CustomStdoutFormatter(opt);
      foreach (var lev in Enum.GetValues<LogLevel>()) {
        var entry= new LogEntry<object> (
          logLevel: lev,
          category: "test.category",
          eventId: new EventId(0, "event"),
          state: "test--message",
          exception: new Exception("test-exception-msg"),
          formatter:(o, s) => o.ToString()
        );
        var tstWriter= new StringWriter();
        formatter.Write<object>(entry, tstScopeProv, tstWriter);
        var log= tstWriter.ToString();
        Assert.EndsWith("\n", log);
        tstout.WriteLine($"'{log.Trim()}'");
        var logCmp= log.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(6 + 2 /*scopes*/, logCmp.Length);
        Assert.True(DateTime.TryParse(logCmp[0], out var d));
        Assert.EndsWith("]", logCmp[1]);
        Assert.EndsWith(":", logCmp[2]);
        Assert.StartsWith("=>", logCmp[3]);
        Assert.StartsWith("=>", logCmp[4]);
        Assert.Equal(entry.State.ToString(), logCmp[5]);
        Assert.EndsWith(":", logCmp[6]);
        Assert.Equal(entry.Exception.Message, logCmp[7]);
      }
    }
  }

}