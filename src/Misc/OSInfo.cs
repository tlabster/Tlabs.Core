using System;
using System.Runtime.InteropServices;

namespace Tlabs.Misc {
  ///<summary>Operating system info</summary>
  public sealed class OSInfo {
    OSInfo() { } //private ctor
    internal static event Action<OSPlatform?> OSPlatformResolved; //resolve event for testing

    ///<summary>Current <see cref="OSPlatform"/></summary>
    public static OSPlatform CurrentPlatform => LazyOSInfo.currentOSPlatform;

    static OSPlatform? handlePlatformResolve(OSPlatform? os) {
      OSPlatformResolved?.Invoke(os);
      return os;
    }

  class LazyOSInfo {
      static LazyOSInfo() { } //Explicit static ctor for *NOT* to marking type with beforefieldinit
      internal static readonly OSPlatform currentOSPlatform= getCurrentOS() ?? throw new GeneralException($"Unknown paltform: '{Environment.OSVersion}");
      static OSPlatform? getCurrentOS() => Environment.OSVersion.Platform switch {
        PlatformID.Unix     => handlePlatformResolve(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux : null),
        PlatformID.Win32NT  => handlePlatformResolve(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows : null),
        PlatformID.MacOSX   => handlePlatformResolve(RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX : null),
        _                   => handlePlatformResolve(null)
      };
    }
  }
}