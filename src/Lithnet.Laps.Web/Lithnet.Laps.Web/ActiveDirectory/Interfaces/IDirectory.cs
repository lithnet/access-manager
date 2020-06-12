using System;
using System.Security.Principal;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.ActiveDirectory
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

        bool IsSidInPrincipalToken(SecurityIdentifier sidToCheck, ISecurityPrincipal principal, SecurityIdentifier targetDomain);
    }
}