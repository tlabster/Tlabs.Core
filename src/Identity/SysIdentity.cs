using System.Security.Principal;

namespace Tlabs.Identity {

  ///<summary>System identity used when running in system backend code.</summary>
  public sealed class SysIdentity : IIdentity {
    private SysIdentity() {} //private ctor
    static SysIdentity() { } //Explicit static ctor for *NOT* to marking type with beforefieldinit

    ///<summary>Singleton instance.</summary>
    public static readonly SysIdentity Instance= new SysIdentity();
    ///<inheritdoc/>
    public string? AuthenticationType => "none";
    ///<inheritdoc/>
    public bool IsAuthenticated => false;
    ///<inheritdoc/>
    public string? Name => "system";
  }
}