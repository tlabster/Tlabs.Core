﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using Tlabs.Misc;
using Tlabs.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace Tlabs.Sys {

  ///<summary>System command line interface</summary>
  public class SystemCli {

    static readonly ILogger log= Tlabs.App.Logger<SystemCli>();
    readonly IReadOnlyDictionary<string, SysCmdTemplates> osPlatformCommands;
    readonly SysCmdTemplates platformCmdTemplates;

    ///<summary>Ctor from <paramref name="sysCmdOptions"/></summary>
    public SystemCli(IOptions<Dictionary<string, SysCmdTemplates>> sysCmdOptions) {
      this.osPlatformCommands= sysCmdOptions.Value.ToDictionary(pair => pair.Key, pair => new SysCmdTemplates(pair.Value));
      if (!this.osPlatformCommands.TryGetValue(OSInfo.CurrentPlatform.ToString(), out var cmdTemplates)) {
        log.LogWarning("Missing command templates for platform {platform}", OSInfo.CurrentPlatform);
        cmdTemplates= new();
      }
      this.platformCmdTemplates= cmdTemplates;
    }

    ///<summary>Enumeration of CLI platform(s).</summary>
    public IEnumerable<OSPlatform> Platforms => osPlatformCommands.Keys.Select(k => OSPlatform.Create(k));

    ///<summary>Enumeration of current platform commands.</summary>
    public IEnumerable<string> PlatformCommands => platformCmdTemplates.CmdLines.Keys;

    ///<summary>Return <see cref="SystemCmd"/> with <paramref name="cmdName"/></summary>
    public SystemCmd Command(string cmdName) {
      if (!platformCmdTemplates.CmdLines.TryGetValue(cmdName, out var cmdLine)) throw new ArgumentException($"Unknown command: '{cmdName}'");
      return new SystemCmd(platformCmdTemplates.Shell, cmdLine).UseWorkingDir(cmdLine.WrkDir);
    }

    ///<summary>Return true if <see cref="SystemCli"/> has <paramref name="cmdName"/> configured.</summary>
    public bool HasCommand(string cmdName) => platformCmdTemplates.CmdLines.ContainsKey(cmdName);

    ///<summary>Service configurator</summary>
    public class Configurator : IConfigurator<IServiceCollection> {
      ///<inheritdoc/>
      public void AddTo(IServiceCollection svc, IConfiguration cfg) {
        svc.Configure<Dictionary<string, SysCmdTemplates>>(cfg.GetSection("sysCommands"));
        svc.AddSingleton<SystemCli>();
      }
    }

  }
}