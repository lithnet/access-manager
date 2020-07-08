using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryDomainConfigurationViewModel : PropertyChangedBase
    {
        private readonly Domain domain;

        private readonly IServiceSettingsProvider serviceSettings;

        private readonly IDirectory directory;

        private readonly SecurityIdentifier domainSid;

        private readonly SecurityIdentifier waagSid = new SecurityIdentifier("S-1-5-32-560");

        private readonly SecurityIdentifier acaoSid = new SecurityIdentifier("S-1-5-32-579");

        private readonly SecurityIdentifier serviceAccountSid;

        private readonly IDialogCoordinator dialogCoordinator;

        public ActiveDirectoryDomainConfigurationViewModel(Domain domain, IServiceSettingsProvider serviceSettings, IDirectory directory, IDialogCoordinator dialogCoordinator)
        {
            this.domain = domain;
            this.dialogCoordinator = dialogCoordinator;
            this.serviceSettings = serviceSettings;
            this.directory = directory;
            this.domainSid = domain.GetDirectoryEntry().GetPropertySid("objectSid");

            this.serviceAccountSid = serviceSettings.GetServiceAccount();

            this.CheckWaagStatus();
            this.CheckAcaoStatus();
        }

        public string WaagStatus { get; set; }

        public string AcaoStatus { get; set; }

        public bool IsWaagMember { get; set; }

        public bool IsNotWaagMember => !this.IsWaagMember;

        public bool IsAcaoMember { get; set; }

        public bool IsNotAcaoMember => !this.IsAcaoMember;

        public string DisplayName => this.domain.Name;

        public bool CanAddToWaag => this.serviceAccountSid != null && !this.IsWaagMember;

        public async Task AddToWaag()
        {
            try
            {
                using PrincipalContext p = new PrincipalContext(ContextType.Domain, this.domain.Name);
                using GroupPrincipal g = GroupPrincipal.FindByIdentity(p, IdentityType.Sid, this.waagSid.ToString());
                g.Members.Add(p, IdentityType.Sid, this.serviceAccountSid.ToString());
                g.Save();

                this.CheckWaagStatus();
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not add the service account to the built-in group 'Windows Authorization Access Group'. Try adding the service account to the group manually\r\n\r\n{ex.Message}");
            }
        }

        public bool CanAddToAcao => this.serviceAccountSid != null && !this.IsAcaoMember;

        public async Task AddToAcao()
        {
            try
            {
                using PrincipalContext p = new PrincipalContext(ContextType.Domain, this.domain.Name);
                using GroupPrincipal g = GroupPrincipal.FindByIdentity(p, IdentityType.Sid, this.acaoSid.ToString());
                g.Members.Add(p, IdentityType.Sid, this.serviceAccountSid.ToString());
                g.Save();
                this.CheckAcaoStatus();
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not add the service account to the built-in group 'Access Control Assistance Operators'. Try adding the service account to the group manually\r\n\r\n{ex.Message}");
            }
        }

        private void CheckAcaoStatus()
        {
            try
            {
                if (this.serviceAccountSid == null)
                {
                    this.IsAcaoMember = false;
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
                    this.IsAcaoMember = false;
                }
            }
            catch (Exception ex)
            {
                this.AcaoStatus = "Could not determine group membership. Try to add the service account to the group";
                this.IsAcaoMember = false;
            }
        }

        private void CheckWaagStatus()
        {
            try
            {
                if (this.serviceAccountSid == null)
                {
                    this.IsWaagMember = false;
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
                    this.IsWaagMember = false;
                }
            }
            catch (Exception ex)
            {
                this.WaagStatus = "Could not determine group membership. Try to add the service account to the group";
                this.IsWaagMember = false;
            }
        }

        private bool IsGroupMember(SecurityIdentifier groupSid, SecurityIdentifier userSid)
        {
            using PrincipalContext p = new PrincipalContext(ContextType.Domain, this.domain.Name);
            using GroupPrincipal g = GroupPrincipal.FindByIdentity(p, IdentityType.Sid, groupSid.ToString());
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
