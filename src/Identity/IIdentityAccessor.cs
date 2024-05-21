using System.Security.Principal;
using System.Security.Claims;

namespace Tlabs.Identity {

  ///<summary>Interface of an accessor to informations regarding the current (user) identity.</summary>
  public interface IIdentityAccessor : IIdentity{
    ///<summary>Current (authenticated) user principal or the default based on <see cref="Identity.SysIdentity"/>.</summary>
    ClaimsPrincipal Principal { get; }
    ///<summary>Current user id or 0 if anonymous.</summary>
    int Id { get; }
    ///<summary>Current roles or null if anonymous.</summary>
    string[] Roles { get; }
    ///<summary>Returns true if user is in role.</summary>
    bool HasRole(string role);
  }
}
