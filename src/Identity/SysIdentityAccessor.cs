
using System.Security.Claims;

namespace Tlabs.Identity {

  ///<summary>Accessor retuning the default <see cref="SysIdentity"/>.</summary>
  public class SysIdentityAccessor : IIdentityAccessor {
    ///<summary>system default <see cref="ClaimsPrincipal"/>.</summary>
    protected ClaimsPrincipal sysPrincipal= new ClaimsPrincipal(new ClaimsIdentity(SysIdentity.Instance));
    ///<inherit/>
    public virtual ClaimsPrincipal Principal => sysPrincipal;
    ///<inherit/>
    public virtual string Name => SysIdentity.Instance.Name;
    ///<inherit/>
    public virtual string AuthenticationType => SysIdentity.Instance.AuthenticationType;
    ///<inherit/>
    public virtual int Id => 0;
    ///<inherit/>
    public virtual string[] Roles => null;
    ///<inherit/>
    public virtual bool HasRole(string role) => false;
  }
}