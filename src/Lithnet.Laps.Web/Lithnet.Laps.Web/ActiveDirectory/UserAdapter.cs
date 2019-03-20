using System.DirectoryServices.AccountManagement;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class UserAdapter: IUser
    {
        private readonly UserPrincipal userPrincipal;

        public UserAdapter(UserPrincipal userPrincipal)
        {
            this.userPrincipal = userPrincipal;
        }

        public string SamAccountName => userPrincipal.SamAccountName;
        public string DistinguishedName => userPrincipal.DistinguishedName;
    }
}