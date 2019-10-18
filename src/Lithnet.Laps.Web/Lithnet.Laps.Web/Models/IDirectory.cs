using System;
using System.Security.Principal;

namespace Lithnet.Laps.Web.Models
{
    public interface IDirectory
    {
        IComputer GetComputer(string computerName);

        PasswordData GetPassword(IComputer computer);

        void SetPasswordExpiryTime(IComputer computer, DateTime time);

        bool IsComputerInOu(IComputer computer, string ou);

        IGroup GetGroup(string groupName);

        IUser GetUser(string userName);

        ISecurityPrincipal GetPrincipal(string principalName);

        bool IsSidInPrincipalToken(SecurityIdentifier targetDomain, ISecurityPrincipal principal, SecurityIdentifier sidToCheck);
    }
}