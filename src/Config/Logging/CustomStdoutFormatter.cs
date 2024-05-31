using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Tlabs.Config {


  ///<summary>Custom stdout formatter.</summary>
  public sealed class CustomStdoutFormatterOptions : ConsoleFormatterOptions {
    ///<summary>Default ctor.</summary>
    public CustomStdoutFormatterOptions() {
      this.TimestampFormat= "O";
      this.IncludeCategory= true;
    }

    ///<summary>Include category with log entry.</summary>
    public bool IncludeCategory { get; set; }

    ///<summary>Defualt min. level.</summary>
    public LogLevel DfltMinimumLevel { get; set; } = LogLevel.Information;

  }
  ///<summary>Custom stdout formatter.</summary>
  ///<remarks>This custom formatter generates log entries with this format:
  ///<code>{timestamp} [{level}] {category}: {message?} {exception?}{newline}</code>
  ///</remarks>
  public sealed class CustomStdoutFormatter : ConsoleFormatter {
    readonly CustomStdoutFormatterOptions options;
    ///<summary>Custom stdout formatter name.</summary>
    public const string NAME= "stdoutFormat";
    ///<summary>Ctor from <paramref name="opt"/>.</summary>
    public CustomStdoutFormatter(IOptions<CustomStdoutFormatterOptions> opt) : base(NAME) {
      this.options= opt.Value;
    }
    ///<inheritdoc/>
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter) {
      var msg= logEntry.Formatter(logEntry.State, logEntry.Exception);
      if (string.IsNullOrEmpty(msg) && null == logEntry.Exception) return;  //nothing to log

      if (!string.IsNullOrEmpty(options.TimestampFormat)) {
        textWriter.Write(App.TimeInfo.Now.ToString(options.TimestampFormat, App.DfltFormat));
        textWriter.Write(' ');
      }
      textWriter.Write(logLevelMark(logEntry.LogLevel));

      if (options.IncludeCategory) {
        textWriter.Write(logEntry.Category);
        textWriter.Write(": ");
      }

      if (options.IncludeScopes && null != scopeProvider) {
        scopeProvider.ForEachScope((scope, state) => {
          state.Write("=>");
          state.Write(scope);
          state.Write(' ');
        }, textWriter);
      }

      if (!string.IsNullOrEmpty(msg)) {
        textWriter.Write(msg);
        textWriter.Write(' ');
      }

      if (null != logEntry.Exception) {
        textWriter.Write(logEntry.Exception.ToString());
      }
      textWriter.WriteLine();
    }

    static string logLevelMark(LogLevel lev) => lev switch {
      LogLevel.Critical => "[CRT] ",
      LogLevel.Error => "[ERR] ",
      LogLevel.Warning => "[WRN] ",
      LogLevel.Information => "[INF] ",
      LogLevel.Debug => "[DBG] ",
      LogLevel.Trace => "[TRC] ",
      _ => "[???] "
    };
  }

}
