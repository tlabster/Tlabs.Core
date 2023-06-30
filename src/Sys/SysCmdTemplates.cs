
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

#nullable enable

namespace Tlabs.Sys {

  ///<summary>System command template(s)</summary>
  public class SysCmdTemplates {
    // public OSPlatform Os { get; set; }

    ///<summary>Default ctor</summary>
    public SysCmdTemplates() { }

    ///<summary>Copy ctor</summary>
    public SysCmdTemplates(SysCmdTemplates sysCmd) {
      this.Shell= (string[])sysCmd.Shell.Clone();
      this.CmdLines= sysCmd.CmdLines?.ToDictionary(pair => pair.Key, pair => new CmdLine {Cmd= pair.Value.Cmd, WrkDir= pair.Value.WrkDir}) ?? new();
    }

    ///<summary>Shell invocation with args</summary>
    public string[] Shell { get; set; }= Array.Empty<string>();

    ///<summary>Named commands to be run with the shell.</summary>
    public Dictionary<string, CmdLine>? CmdLines { get; set; }

    ///<summary>Command line.</summary>
    public class CmdLine {
      static readonly string[] emptyCmd= Array.Empty<string>();
      ///<summary>Command with arguments.</summary>
      public string[] Cmd { get; set; }= emptyCmd;
      ///<summary>Working directory for command (optional).</summary>
      public string WrkDir { get; set; }= "";

      ///<summary>Resolve <paramref name="shell"/> and <see cref="CmdLine.Cmd"/> with <paramref name="args"/>.</summary>
      ///<remarks>
      ///Any element of <paramref name="shell"/> or <see cref="CmdLine.Cmd"/> could contain a special format item defined here:
      ///https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-6.0#the-format-item
      ///<para>
      ///<paramref name="shell"/> elements are sesolved using <see cref="CmdLine.Cmd"/>
      ///</para>
      ///<para>
      ///<see cref="CmdLine.Cmd"/> all elements are resolved using <paramref name="args"/>
      ///</para>
      ///</remarks>
      public (string cmd, string[] args) ResolveArguments(string[] shell, params string[] args) {
        if (0 == shell.Length) throw new ArgumentException("No shell specified");
        var lst= new List<string>();
        foreach (var s in shell) lst.Add(string.Format(CultureInfo.InvariantCulture, s, this.Cmd));
        for (var l= 1; l < Cmd.Length; ++l) lst.Add(string.Format(CultureInfo.InvariantCulture, Cmd[l], args));
        var res= lst.ToArray();
        return (res[0], res[1..]);
      }
    }
  }

}
