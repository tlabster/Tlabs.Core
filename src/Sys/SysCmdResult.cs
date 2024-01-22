
using System;
using System.IO;
using System.Threading;

#nullable enable

namespace Tlabs.Sys {

  ///<summary>System command result</summary>
  public class SysCmdResult : IDisposable {
    ///<summary>true if disposed</summary>
    protected int isDisposed;

    ///<summary>Start time</summary>
    public DateTime StartTime { get; set; }
    ///<summary>Exit time</summary>
    public DateTime ExitTime { get; set; }
    ///<summary>Exit code</summary>
    public int ExitCode { get; set; }

    ///<summary>Internal dispose</summary>
    protected virtual void Dispose(bool disposing) {
      if (0 != Interlocked.Exchange(ref isDisposed, 1)) return;
      // if (disposing) {
      //   // TODO: dispose managed state (managed objects)
      // }
    }

    ///<inheritdoc/>
    public void Dispose() {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }

  ///<summary>Buffered system command result</summary>
  public class SysBufferedCmdResult : SysCmdResult {
    ///<summary>Standard out stream</summary>
    public TextReader StdOut { get; set; }= TextReader.Null;
    ///<summary>Standard error stream</summary>
    public TextReader StdErr { get; set; }= TextReader.Null;

    ///<inheritdoc/>
    protected override void Dispose(bool disposing) {
      if (0 == isDisposed && disposing) {
        StdOut.Dispose();
        StdErr.Dispose();
      }
      base.Dispose(disposing);
    }
  }

  ///<summary>Buffered system command result</summary>
  public class StdCmdIO {
    ///<summary>Standard out stream</summary>
    public TextReader StdOut { get; set; }= TextReader.Null;
    ///<summary>Standard error stream</summary>
    public TextReader StdErr { get; set; }= TextReader.Null;
    ///<summary>Standard input stream</summary>
    public TextWriter StdIn { get; set; }= TextWriter.Null;

    ///<summary>Close all IO streams.</summary>
    public void CloseAll() {
      StdOut.Close();
      StdErr.Close();
      StdIn.Close();
    }
  }
}
