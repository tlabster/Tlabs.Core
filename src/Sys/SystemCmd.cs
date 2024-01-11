using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Tlabs.Misc;

#nullable enable

namespace Tlabs.Sys {

  ///<summary>System command</summary>
  /*** TODO: This is still WIP...
   * - add more "run" modes (currently only BufferedRun) e.g. Progress() returning IAsyncEnumerable<state>...
   * - support input redirection
   * -...
   */
  public class SystemCmd {
    static readonly ILogger log = Tlabs.App.Logger<SystemCmd>();

    readonly string[] shellCmd;
    readonly SysCmdTemplates.CmdLine cmdLine;
    // [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private member", Justification = "Not production code.")]
    string workingDirPath= "";
    string[] arguments= Array.Empty<string>();

    ///<summary>Ctor from <paramref name="shellCmd"/> and <paramref name="cmdLine"/></summary>
    public SystemCmd(string[] shellCmd, SysCmdTemplates.CmdLine? cmdLine= null) {
      this.shellCmd= shellCmd;
      this.cmdLine= cmdLine ?? new();
    }

    ///<summary>Effective command line (fully resolved)</summary>
    public (string cmd, string[] args) CmdLine => this.cmdLine.ResolveArguments(this.shellCmd, this.arguments);

    ///<summary>Use working directory</summary>
    public SystemCmd UseWorkingDir(string path) {
      if (!Path.IsPathRooted(path))
        path= Path.Combine(App.ContentRoot, path);
      if (!Directory.Exists(path)) throw new ArgumentException($"Invalid path: {path}");
      this.workingDirPath= path;
      return this;
    }

    ///<summary>Use arguments</summary>
    public SystemCmd UseArguments(params string[] args) {
      this.arguments= args;
      return this;
    }

    ///<summary>Run this command with a new OS process and buffer its stdout and stderr stream in the returned <see cref="SysBufferedCmdResult"/> </summary>
    public async Task<SysBufferedCmdResult> BufferedRun(CancellationToken ctk= default) {
      var bufferedRes= new SysBufferedCmdResult();

      using var proc= createProcess(bufferedRes, redirStdOut: true, redirStdErr: true);

      /* Attach redirected output stream:
       */
      var stdErr= new StringWriter();
      var stdOut= new StringWriter();
      proc.ErrorDataReceived+= (_, ev) => stdErr.WriteLine(ev.Data);
      proc.OutputDataReceived+= (_, ev) => stdOut.WriteLine(ev.Data);
      proc.BeginErrorReadLine();
      proc.BeginOutputReadLine();

      await proc.WaitForExitAsync(ctk);
      bufferedRes.ExitCode= proc.ExitCode;
      bufferedRes.StdErr= new StringReader(stdErr.ToString());
      bufferedRes.StdOut= new StringReader(stdOut.ToString());

      return bufferedRes;
    }

    ///<summary>Run this command with a new OS process and optional redirected <paramref name="stdIO"/></summary>
    ///<remarks>Any redirected <paramref name="stdIO"/> streams can be read / written until the returned <see cref="Task"/> completes.
    ///</remarks>
    ///<returns><see cref="SysCmdResult"/></returns>
    public async Task<SysCmdResult> Run(StdCmdIO? stdIO= null, bool redirStdOut= false, bool redirStdErr= false, bool redirStdIn= false, CancellationToken ctk= default) {
      var cmdRes= new SysCmdResult();
      using var proc= createProcess(cmdRes, redirStdOut, redirStdErr, redirStdIn);

      /* Attach redirected output stream:
       */
      if (null != stdIO) {
        if (redirStdOut) {
          var br= new BufferedReader(ctk);
          stdIO.StdOut= br;
          proc.OutputDataReceived+= (_, ev) => br.BufferLine(ev.Data);
          br.Closing+= () => proc.CancelOutputRead();
          proc.BeginOutputReadLine();
        }
        if (redirStdErr) {
          var br= new BufferedReader(ctk);
          stdIO.StdErr= br;
          proc.ErrorDataReceived+= (_, ev) => br.BufferLine(ev.Data);
          br.Closing+= () => proc.CancelErrorRead();
          proc.BeginErrorReadLine();
        }
        if (redirStdIn) {
          stdIO.StdIn= proc.StandardInput;
        }
      }

      try {
        await proc.WaitForExitAsync(ctk);
        cmdRes.ExitCode= proc.ExitCode;
      }
      finally {
        stdIO?.CloseAll();
        if (!proc.HasExited) proc.Kill(entireProcessTree: true);
      }
      return cmdRes;
    }


    private Process createProcess(SysCmdResult cmdRes, bool redirStdOut= false, bool redirStdErr= false, bool redirStdIn= false) {
      var run= this.CmdLine;

      var startInfo= new ProcessStartInfo {
        FileName= run.cmd,
        CreateNoWindow= true,
        RedirectStandardError= redirStdErr,
        RedirectStandardOutput= redirStdOut,
        RedirectStandardInput= redirStdIn,
        UseShellExecute= false,
        WorkingDirectory= string.IsNullOrEmpty(workingDirPath) ? this.cmdLine.WrkDir : workingDirPath
      };
      foreach (var arg in run.args) startInfo.ArgumentList.Add(arg);

      var proc= new Process {
        StartInfo= startInfo,
        EnableRaisingEvents= true
      };
      try {
        log.LogInformation("Starting process: {proc} {args}", run.cmd, string.Join(" ", run.args));
        return startProcess(proc, cmdRes);
      }
      catch (Exception) {
        proc.Dispose();
        throw;
      }
    }

    private static Process startProcess(Process proc, SysCmdResult cmdRes) {
      var cmd= proc.StartInfo.FileName;
      try {
        proc.Exited+= (_, _) => cmdRes.ExitTime= DateTime.Now;  //Process.ExitTime could become inaccessible after exit...
        cmdRes.StartTime= DateTime.Now;                         //Process.StartTime could become inaccessible after exit...
        if (!proc.Start()) throw new InvalidOperationException($"{cmd} was not started - but reused?");
      }
      catch (System.ComponentModel.Win32Exception e) { throw new InvalidOperationException($"Failed to start {cmd}", e); }

      return proc;
    }
  }

}