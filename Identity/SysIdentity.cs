using System;
using System.Security.Principal;

namespace Tlabs.Identity {

  ///<summary>System identity used when running in system backend code.</summary>
  public sealed class SysIdentity : IIdentity {
    private SysIdentity() {} //private ctor
    static SysIdentity() { } //Explicit static ctor for *NOT* to marking type with beforefieldinit

    ///<summary>Singleton instance.</summary>
    public static readonly SysIdentity Instance= new SysIdentity();
    ///<inherit/>
    public string AuthenticationType => "none";
    ///<inherit/>
    public bool IsAuthenticated => false;
    ///<inherit/>
    public string Name => "system";
  }
}