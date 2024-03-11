using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Tlabs.Config {
  ///<summary>Configures a console logger.</summary>
  public class SysLoggingConfigurator : IConfigurator<ILoggingBuilder> {
    ///<inheritdoc/>
    public void AddTo(ILoggingBuilder log, IConfiguration cfg) {
      var optConfig= cfg.GetSection("options");
      log.Services.Configure<ConsoleFormatterOptions>(optConfig);

     log.AddSystemdConsole();
    }
  }

  ///<summary>Configures a Serilog file logger.</summary>
  public class FileLoggingConfigurator : IConfigurator<ILoggingBuilder> {
    ///<inheritdoc/>
    public void AddTo(ILoggingBuilder log, IConfiguration cfg) {
      Environment.SetEnvironmentVariable("EXEPATH", Path.GetDirectoryName(App.MainEntryPath));
      var optConfig= cfg.GetSection("options");
      log.AddFile(optConfig);
    }
  }

  ///<summary>Configures a console logger.</summary>
  public class StdoutLoggingConfigurator : IConfigurator<ILoggingBuilder> {
    ///<inheritdoc/>
    public void AddTo(ILoggingBuilder log, IConfiguration cfg) {
      var optConfig= cfg.GetSection("options");
      log.Services.Configure<CustomStdoutFormatterOptions>(optConfig);

      log.AddConsole(opt => opt.FormatterName= CustomStdoutFormatter.NAME)
         .AddConsoleFormatter<CustomStdoutFormatter, CustomStdoutFormatterOptions>();
    }
  }


  ///<summary>Configures a <see cref="System.Diagnostics.Tracing.EventSource"/> logger.</summary>
  public class EventSourceLoggingConfigurator : IConfigurator<ILoggingBuilder> {
    ///<inheritdoc/>
    public void AddTo(ILoggingBuilder log, IConfiguration cfg) {
      log.AddEventSourceLogger();
      log.Services.Configure<LoggerFactoryOptions>(opt => opt.ActivityTrackingOptions=   ActivityTrackingOptions.SpanId
                                                                                       | ActivityTrackingOptions.TraceId
                                                                                       | ActivityTrackingOptions.ParentId
      );
    }
  }

}