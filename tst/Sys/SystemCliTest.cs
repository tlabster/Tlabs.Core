using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Tlabs.Misc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
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
    public async void BufferedHelloTest() {
      var cli0= new SystemCli(new TstOptions<Dictionary<string, SysCmdTemplates>>(new()));
      Assert.Throws<ArgumentException>(() => cli0.Command("missing"));

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
  }
}