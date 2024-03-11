using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Tlabs.Misc;
using Microsoft.Extensions.Logging;

namespace Tlabs.Config {
  /// <summary>Distributer of <see cref="Console"/> output.</summary>
  public class ConsoleOutputDistributor : TextWriter {
    readonly Dictionary<Stream, TextWriter> writers= new();
    /// <summary>Default ctor</summary>
    public ConsoleOutputDistributor() {
      writers[Stream.Null]= Console.Out;
      this.Encoding= Console.Out.Encoding;
      Console.SetOut(this);    //capture stdout
    }

    /// <summary>Add <paramref name="strm"/></summary>
    public void AddStream(Stream strm) {
      var wr= new StreamWriter(strm, this.Encoding, 512);
      wr.AutoFlush= true;
      lock (writers) writers[strm]= wr;
    }
    /// <summary>Remove <paramref name="strm"/></summary>
    public void RemoveStream(Stream strm) {
      lock (writers) writers.Remove(strm);
    }
    /// <inheritdoc/>
    public override Encoding Encoding { get; }
    /// <inheritdoc/>
    public override void Write(char c) {
      foreach (var wr in writers.Values) {
        wr.Write(c);
      }
    }
    /// <inheritdoc/>
    public override void Write(string? s) {
      foreach (var wr in writers.Values) {
        wr.Write(s);
      }
    }

    /// <summary>Configurator</summary>
    public class Configurator : IConfigurator<IServiceCollection>, IConfigurator<ILoggingBuilder> {
      ///<inheritdoc/>
      public void AddTo(IServiceCollection services, IConfiguration cfg) {
        services.AddSingleton<ConsoleOutputDistributor>(Singleton<ConsoleOutputDistributor>.Instance);
      }
      ///<inheritdoc/>
      public void AddTo(ILoggingBuilder builder, IConfiguration cfg) {
        /* Create an instance to capture the console log before console logger (from builder.AddConsole()) gets its
         * internal Console.Out copy...
         */
        var consoleout= Singleton<ConsoleOutputDistributor>.Instance;
      }
    }
  }
}