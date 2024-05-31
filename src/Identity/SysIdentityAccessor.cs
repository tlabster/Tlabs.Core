
using System;
using System.Security.Claims;

namespace Tlabs.Identity {

  ///<summary>Accessor retuning the default <see cref="SysIdentity"/>.</summary>
  public class SysIdentityAccessor : IIdentityAccessor {
    ///<summary>system default <see cref="ClaimsPrincipal"/>.</summary>
    protected ClaimsPrincipal sysPrincipal= new ClaimsPrincipal(new ClaimsIdentity(SysIdentity.Instance));
    ///<inheritdoc/>
    public virtual ClaimsPrincipal Principal => sysPrincipal;
    ///<inheritdoc/>
    public virtual string? Name => SysIdentity.Instance.Name;
    ///<inheritdoc/>
    public virtual string? AuthenticationType => SysIdentity.Instance.AuthenticationType;
    ///<inheritdoc/>
    public bool IsAuthenticated => SysIdentity.Instance.IsAuthenticated;
    ///<inheritdoc/>
    public virtual int Id => 0;
    ///<inheritdoc/>
    public virtual string[] Roles => Array.Empty<string>();
    ///<inheritdoc/>
    public virtual bool HasRole(string role) => false;
  }
}