using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
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
        public SecurityIdentifier Sid => userPrincipal.Sid;
        public string DisplayName => userPrincipal.DisplayName;
        public string UserPrincipalName => userPrincipal.UserPrincipalName;
        public string Description => userPrincipal.Description;
        public string EmailAddress => userPrincipal.EmailAddress;
        public Guid? Guid => userPrincipal.Guid;
        public string GivenName => userPrincipal.GivenName;
        public string Surname => userPrincipal.Surname;
    }
}