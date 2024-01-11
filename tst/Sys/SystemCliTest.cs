using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Xunit;
using Xunit.Abstractions;

namespace Tlabs.Sys.Tests {

  public class SystemCliTest {

    class TstOptions<T> : IOptions<T> where T : class{
      public TstOptions(T opt) => Value= opt;
      public T Value { get; }
    }

    readonly Dictionary<string, SysCmdTemplates> sysCmdTemplates= new() {
      [OSPlatform.Windows.ToString()]= new() {
        Shell= new string[] {"cmd.exe", "/c", "{0}" },
        CmdLines= new() {
          ["hello"]= new() {
            Cmd= new string[] {"hello.cmd", "{0}", "{1}"},
            WrkDir= "rsc"
          }
        }
      },
      [OSPlatform.Linux.ToString()]= new() {
        Shell= new string[] { "bash", "{0}" },
        CmdLines= new() {
          ["hello"]= new() {
            Cmd= new string[] { "hello.sh", "{0}", "{1}" },
            WrkDir= "rsc"
          }
        }
      }
    };

    readonly ITestOutputHelper tstout;
    public SystemCliTest(ITestOutputHelper tstout) {
      this.tstout= tstout;
    }

    [Fact]
    public void RawCmdTest() {
      var cmd= new SystemCmd(new string[]{"cmd.exe", "/?"});
      var cln= cmd.CmdLine;
      Assert.StartsWith("cmd.exe", cln.cmd);
      Assert.Equal(1, cln.args.Length);
    }

    // [Fact]
    // public async Task RawCmdTest2() {
    //   // var cmd= new SystemCmd(new string[]{@"D:\opt\Tools\echo.exe", "hello"});
    //   var cmd = new SystemCmd(new string[] { "cmd.exe", "/?" });
    //   var cln= cmd.CmdLine;
    //   var cmdIO= new StdCmdIO();
    //   var cmdTsk= cmd.Run(cmdIO, redirStdOut: true);

    //   // for (string? line= await cmdIO.StdOut.ReadLineAsync(); null != line; line= await cmdIO.StdOut.ReadLineAsync()) {
    //   //   tstout.WriteLine(line);
    //   // }
    //   for (string? line= await cmdIO.StdOut.ReadLineAsync();
    //        null != line;
    //        line= await cmdIO.StdOut.ReadLineAsync()) {
    //     tstout.WriteLine(line);
    //   }
    //   tstout.WriteLine("stdOut closed");
    //   await cmdTsk;
    // }

    [Fact]
    public void InvalidCliCmdTest() {
      var cli0= new SystemCli(new TstOptions<Dictionary<string, SysCmdTemplates>>(new()));
      Assert.Throws<ArgumentException>(() => cli0.Command("missing"));
    }

    [Fact]
    public async void BufferedHelloTest() {
      var cli= new SystemCli(new TstOptions<Dictionary<string, SysCmdTemplates>>(this.sysCmdTemplates));
      var cmd= cli.Command("hello");
      var res= await cmd.UseArguments("one", "two")
                        .BufferedRun();
      var outLine= res.StdOut.ReadLine();
      tstout.WriteLine(outLine);
      Assert.Equal("Hello, world!", outLine);
      outLine= res.StdOut.ReadLine();
      tstout.WriteLine(outLine);
      Assert.Equal("Params: \"one\" \"two\"", outLine);
      Assert.Empty(res.StdOut.ReadToEnd().Trim());

      tstout.WriteLine(res.StdErr.ReadToEnd());
      Assert.Equal(0, res.ExitCode);
    }

    [Fact]
    public async void AsyncHelloTest() {
      var cli= new SystemCli(new TstOptions<Dictionary<string, SysCmdTemplates>>(this.sysCmdTemplates));
      var cmd= cli.Command("hello");
      var cmdIO= new StdCmdIO();
      var cmdTsk= cmd.UseArguments("one", "two")
                     .Run(cmdIO, redirStdOut: true);
      var outLine= await cmdIO.StdOut.ReadLineAsync();
      tstout.WriteLine(outLine);
      Assert.Equal("Hello, world!", outLine);
      outLine= await cmdIO.StdOut.ReadLineAsync();
      tstout.WriteLine(outLine);
      Assert.Equal("Params: \"one\" \"two\"", outLine);
      var endTsk= cmdIO.StdOut.ReadToEndAsync();

      var res= await cmdTsk;
      Assert.Equal(0, res.ExitCode);
      Assert.Empty((await endTsk).Trim());

    }

  }
}