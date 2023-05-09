﻿
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable

namespace Tlabs.Sys {

  ///<summary>System command result</summary>
  public class SysCmdResult : IDisposable {
    ///<summary>true if disposed</summary>
    protected bool isDisposed;

    ///<summary>Start time</summary>
    public DateTime StartTime { get; set; }
    ///<summary>Exit time</summary>
    public DateTime ExitTime { get; set; }
    ///<summary>Exit code</summary>
    public int ExitCode { get; set; }

    ///<summary>Internal dispose</summary>
    protected virtual void Dispose(bool disposing) {
      if (!isDisposed) {
        // if (disposing) {
        //   // TODO: dispose managed state (managed objects)
        // }
        isDisposed= true;
      }
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
      if (!isDisposed && disposing) {
        StdOut.Dispose();
        StdErr.Dispose();
      }
      base.Dispose(disposing);
    }
  }

}
