using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Directory
{
    public sealed class GroupAdapter: IGroup
    {
        private GroupPrincipal groupPrincipal;

        public GroupAdapter(GroupPrincipal groupPrincipal)
        {
            this.groupPrincipal = groupPrincipal;
        }

        public Guid? Guid => groupPrincipal.Guid;

        public SecurityIdentifier Sid => groupPrincipal.Sid;
    }
}
