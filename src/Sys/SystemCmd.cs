using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

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
    string workingDirPath= "";
    string[] arguments= Array.Empty<string>();
    internal SystemCmd(string[] shellCmd, SysCmdTemplates.CmdLine cmdLine) {
      this.shellCmd= shellCmd;
      this.cmdLine= cmdLine;
    }

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

      using var proc= createProcess(bufferedRes, redirect: true);

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

    private Process createProcess(SysCmdResult cmdRes, bool redirect) {
      var run= this.cmdLine.ResolveArguments(this.shellCmd, this.arguments);

      var startInfo= new ProcessStartInfo {
        FileName= run.cmd,
        CreateNoWindow= true,
        RedirectStandardError= redirect,
        RedirectStandardOutput= redirect,
        UseShellExecute= false,
        WorkingDirectory= this.cmdLine.WrkDir
      };
      foreach (var arg in run.args) startInfo.ArgumentList.Add(arg);
      var proc= new Process {
        StartInfo= startInfo,
        EnableRaisingEvents= true
      };
      proc.Exited+= (_, _) => cmdRes.ExitTime= DateTime.Now;  //Process.ExitTime could become inaccessible after exit...

      log.LogInformation("Starting process: {proc} {args}", run.cmd, string.Join(" ", run.args));
      return startProcess(proc, cmdRes);
    }

    private static Process startProcess(Process proc, SysCmdResult cmdRes) {
      var cmd= proc.StartInfo.FileName;
      try {
        if (!proc.Start()) throw new InvalidOperationException($"{cmd} was not started - but reused?");
        cmdRes.StartTime= DateTime.Now;   //Process.StartTime could become inaccessible after exit...
      }
      catch (System.ComponentModel.Win32Exception e) { throw new InvalidOperationException($"Failed to start {cmd}", e); }

      return proc;
    }
  }

}