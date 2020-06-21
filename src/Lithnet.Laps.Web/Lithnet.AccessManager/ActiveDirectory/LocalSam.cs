using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public sealed class LocalSam : ILocalSam
    {
        private readonly ILogger<LocalSam> logger;

        public LocalSam(ILogger<LocalSam> logger)
        {
            this.logger = logger;
        }

        public SecurityIdentifier GetWellKnownSid(WellKnownSidType sidType)
        {
            return NativeMethods.CreateWellKnownSid(sidType);
        }

        public SecurityIdentifier GetWellKnownSid(WellKnownSidType sidType, SecurityIdentifier domainSid)
        {
            return NativeMethods.CreateWellKnownSid(sidType, domainSid.AccountDomainSid);
        }

        public string GetMachineNetbiosDomainName()
        {
            var result = NativeMethods.GetWorkstationInfo(null);
            return result.LanGroup;
        }

        public string GetMachineNTAccountName()
        {
            var result = NativeMethods.GetWorkstationInfo(null);
            return $"{result.LanGroup}\\{result.ComputerName}";
        }

        public IList<SecurityIdentifier> GetLocalGroupMembers(string name)
        {
            return NativeMethods.GetLocalGroupMembers(name);
        }

        public string GetBuiltInAdministratorsGroupName()
        {
            NTAccount adminGroup = (NTAccount)this.GetWellKnownSid(WellKnownSidType.BuiltinAdministratorsSid).Translate(typeof(NTAccount));
            return adminGroup.Value.Split('\\')[1];
        }

        public void AddLocalGroupMember(string groupName, SecurityIdentifier member)
        {
            NativeMethods.AddLocalGroupMember(groupName, member);
        }

        public void RemoveLocalGroupMember(string groupName, SecurityIdentifier member)
        {
            NativeMethods.RemoveLocalGroupMember(groupName, member);
        }

        public void UpdateLocalGroupMembership(string groupName, IEnumerable<SecurityIdentifier> membersToAdd, IEnumerable<SecurityIdentifier> membersToRemove, bool ignoreErrors)
        {
            if (membersToAdd != null)
            {
                foreach (var member in membersToAdd)
                {
                    try
                    {
                        this.AddLocalGroupMember(groupName, member);
                        this.logger.LogInformation("Added member {member} to group {groupName}", member, groupName);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Failed to add member {member} to group {groupName}", member, groupName);
                        if (!ignoreErrors)
                        {
                            throw;
                        }
                    }
                }
            }

            if (membersToRemove != null)
            {
                foreach (var member in membersToRemove)
                {
                    try
                    {
                        this.RemoveLocalGroupMember(groupName, member);
                        this.logger.LogInformation("Removed member {member} from group {groupName}", member, groupName);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Failed to remove member {member} from group {groupName}", member, groupName);
                        if (!ignoreErrors)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        public void UpdateLocalGroupMembership(string groupName, IEnumerable<SecurityIdentifier> expectedMembers, bool allowOthers, bool ignoreErrors)
        {
            IList<SecurityIdentifier> currentMembers = this.GetLocalGroupMembers(groupName);
            IEnumerable<SecurityIdentifier> membersToAdd = expectedMembers.Except(currentMembers);
            IEnumerable<SecurityIdentifier> membersToRemove = allowOthers ? null : currentMembers.Except(expectedMembers);

            this.UpdateLocalGroupMembership(groupName, membersToAdd, membersToRemove, ignoreErrors);
        }

        public bool IsDomainController()
        {
            var info = NativeMethods.GetServerInfo(null);

            return (info.Type & ServerTypes.DomainCtrl) == ServerTypes.DomainCtrl || (info.Type & ServerTypes.BackupDomainCtrl) == ServerTypes.BackupDomainCtrl;
        }

        public void SetLocalAccountPassword(SecurityIdentifier sid, string password)
        {
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                using (var user = UserPrincipal.FindByIdentity(context, IdentityType.Sid, sid.ToString()))
                {
                    user.SetPassword(password);
                }
            }
        }
    }
}