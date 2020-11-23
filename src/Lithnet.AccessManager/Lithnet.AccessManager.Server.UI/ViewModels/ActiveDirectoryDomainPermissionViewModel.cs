using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryDomainPermissionViewModel : PropertyChangedBase
    {
        private readonly Domain domain;

        private readonly SecurityIdentifier waagSid = new SecurityIdentifier("S-1-5-32-560");

        private readonly SecurityIdentifier acaoSid = new SecurityIdentifier("S-1-5-32-579");

        private readonly SecurityIdentifier serviceAccountSid;

        private readonly ILogger logger;

        public ActiveDirectoryDomainPermissionViewModel(Domain domain, IWindowsServiceProvider windowsServiceProvider, ILogger<ActiveDirectoryDomainPermissionViewModel> logger)
        {
            this.logger = logger;
            this.domain = domain;
            this.serviceAccountSid = windowsServiceProvider.GetServiceSid();
        }

        public void RefreshGroupMembership()
        {
            this.CheckWaagStatus();
            this.CheckAcaoStatus();
        }

        public async Task RefreshGroupMembershipAsync()
        {
            await Task.Run(this.RefreshGroupMembership);
        }

        public string WaagStatus { get; set; }

        public string AcaoStatus { get; set; }

        public bool IsWaagMember { get; set; }

        public bool IsNotWaagMember { get; set; }

        public bool IsAcaoMember { get; set; }

        public bool IsNotAcaoMember { get; set; }

        public string Name => this.domain.Name;

        public string ForestName => this.domain.Forest.Name;

        private void CheckAcaoStatus()
        {
            try
            {
                this.AcaoLookupInProgress = true;
                this.IsAcaoMember = false;
                this.IsNotAcaoMember = false;
                this.AcaoStatus = "Checking...";

                if (this.serviceAccountSid == null)
                {
                    this.IsNotAcaoMember = true;
                    this.AcaoStatus = "Could not determine service account";
                    return;
                }

                if (!this.GroupExists(this.acaoSid))
                {
                    this.AcaoStatus = "Group not found in domain";
                    return;
                }

                if (this.IsGroupMember(this.acaoSid, this.serviceAccountSid))
                {
                    this.AcaoStatus = "Group membership confirmed";
                    this.IsAcaoMember = true;
                }
                else
                {
                    this.AcaoStatus = "Group membership not found";
                    this.IsNotAcaoMember = true;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGroupMembershipLookupError, ex, "Group membership lookup error");
                this.AcaoStatus = "Group membership lookup error";
                this.IsNotAcaoMember = true;
            }
            finally
            {
                this.AcaoLookupInProgress = false;
            }
        }

        public bool WaagLookupInProgress { get; set; }

        public bool AcaoLookupInProgress { get; set; }

        private void CheckWaagStatus()
        {
            try
            {
                this.IsWaagMember = false;
                this.IsNotWaagMember = false;
                this.WaagLookupInProgress = true;
                this.WaagStatus = "Checking...";

                if (this.serviceAccountSid == null)
                {
                    this.WaagStatus = "Could not determine service account";
                    this.IsNotWaagMember = true;
                    return;
                }

                if (!this.GroupExists(this.waagSid))
                {
                    this.AcaoStatus = "Group not found";
                    return;
                }

                if (this.IsGroupMember(this.waagSid, this.serviceAccountSid))
                {
                    this.WaagStatus = "Group membership confirmed";
                    this.IsWaagMember = true;
                }
                else
                {
                    this.WaagStatus = "Group membership not found";
                    this.IsNotWaagMember = true;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGroupMembershipLookupError, ex, "Group membership lookup error");
                this.WaagStatus = "Group membership lookup error";
                this.IsNotWaagMember = true;
            }
            finally
            {
                this.WaagLookupInProgress = false;
            }
        }

        private bool GroupExists(SecurityIdentifier groupSid)
        {
            using PrincipalContext p = new PrincipalContext(ContextType.Domain, this.domain.Name);
            using GroupPrincipal g = GroupPrincipal.FindByIdentity(p, IdentityType.Sid, groupSid.ToString());

            return g != null;
        }

        private bool IsGroupMember(SecurityIdentifier groupSid, SecurityIdentifier userSid)
        {
            using PrincipalContext p = new PrincipalContext(ContextType.Domain, this.domain.Name);
            using GroupPrincipal g = GroupPrincipal.FindByIdentity(p, IdentityType.Sid, groupSid.ToString());

            if (g == null)
            {
                this.logger.LogTrace($"The group {groupSid.Value} does not exist");
                return false;
            }

            return IsGroupMember(g, userSid, new HashSet<string>());
        }

        private bool IsGroupMember(GroupPrincipal g, SecurityIdentifier userSid, HashSet<string> searchedGroups)
        {
            if (!(searchedGroups.Add(g.Sid.ToString())))
            {
                return false;
            }

            foreach (var member in g.GetMembers())
            {
                if (member.Sid == userSid)
                {
                    return true;
                }

                if (member is GroupPrincipal g2)
                {
                    if (IsGroupMember(g2, userSid, searchedGroups))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
