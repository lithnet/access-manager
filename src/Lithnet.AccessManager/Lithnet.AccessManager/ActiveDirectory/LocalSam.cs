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

        private string machineNtAccountName;

        private string machineNetbiosName;
        
        private bool? isDC;

        public LocalSam(ILogger<LocalSam> logger)
        {
            this.logger = logger;
        }

        public SecurityIdentifier GetWellKnownSid(WellKnownSidType sidType)
        {
            return NativeMethods.CreateWellKnownSid(sidType);
        }

        public SecurityIdentifier GetWellKnownSid(string server, WellKnownSidType sidType)
        {
            var localMachineAuthoritySid = NativeMethods.GetLocalMachineAuthoritySid(server);
            return new SecurityIdentifier(sidType, localMachineAuthoritySid);
        }

        public SecurityIdentifier GetWellKnownSid(WellKnownSidType sidType, SecurityIdentifier domainSid)
        {
            return new SecurityIdentifier(sidType, domainSid.AccountDomainSid);
        }

        public SecurityIdentifier GetLocalMachineAuthoritySid(string server)
        {
            return NativeMethods.GetLocalMachineAuthoritySid(server);
        }

        public string GetMachineNetbiosDomainName()
        {
            if (this.machineNetbiosName == null)
            {
                var result = NativeMethods.GetWorkstationInfo(null);
                this.machineNetbiosName = result.LanGroup;
            }

            return this.machineNetbiosName;
        }

        public string GetMachineNTAccountName()
        {
            if (this.machineNtAccountName == null)
            {
                var result = NativeMethods.GetWorkstationInfo(null);
                this.machineNtAccountName = $"{result.LanGroup}\\{result.ComputerName}";
            }

            return this.machineNtAccountName;
        }

        public IList<SecurityIdentifier> GetLocalGroupMembers(string name)
        {
            return this.GetLocalGroupMembers(null, name);
        }

        public IList<SecurityIdentifier> GetLocalGroupMembers(string server, string name)
        {
            return NativeMethods.GetLocalGroupMembers(server, name);
        }

        public string GetBuiltInAdministratorsGroupName()
        {
            NTAccount adminGroup = (NTAccount)this.GetWellKnownSid(WellKnownSidType.BuiltinAdministratorsSid).Translate(typeof(NTAccount));
            return adminGroup.Value.Split('\\')[1];
        }
        public string GetBuiltInAdministratorsGroupName(string server)
        {
            return NativeMethods.GetBuiltInAdministratorsGroupName(server);
        }
        public string GetBuiltInAdministratorsGroupNameOrDefault(string server)
        {
            try
            {
                return this.GetBuiltInAdministratorsGroupName(server);
            }
            catch (Exception ex)
            {
                this.logger.LogTrace(ex, "Unable to get built-in administrators group name from computer {server}, falling back to default value", server);
            }

            return "Administrators";
        }

        public void AddLocalGroupMember(string groupName, SecurityIdentifier member)
        {
            NativeMethods.AddLocalGroupMember(groupName, member);
        }

        public void RemoveLocalGroupMember(string groupName, SecurityIdentifier member)
        {
            NativeMethods.RemoveLocalGroupMember(groupName, member);
        }

        public bool UpdateLocalGroupMembership(string groupName, IEnumerable<SecurityIdentifier> membersToAdd, IEnumerable<SecurityIdentifier> membersToRemove, bool ignoreErrors)
        {
            bool modifiedMembership = false;

            if (membersToAdd != null)
            {
                foreach (var member in membersToAdd)
                {
                    try
                    {
                        this.AddLocalGroupMember(groupName, member);
                        modifiedMembership = true;
                        this.logger.LogInformation(EventIDs.LocalSamGroupMemberAdded, "Added member {member} to group {groupName}", member, groupName);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(EventIDs.LocalSamGroupMemberAddFailed, ex, "Failed to add member {member} to group {groupName}", member, groupName);
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
                        modifiedMembership = true;
                        this.logger.LogInformation(EventIDs.LocalSamGroupMemberRemoved, "Removed member {member} from group {groupName}", member, groupName);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(EventIDs.LocalSamGroupMemberRemoveFailed, ex, "Failed to remove member {member} from group {groupName}", member, groupName);
                        if (!ignoreErrors)
                        {
                            throw;
                        }
                    }
                }
            }

            return modifiedMembership;
        }

        public bool UpdateLocalGroupMembership(string groupName, IEnumerable<SecurityIdentifier> expectedMembers, bool allowOthers, bool ignoreErrors)
        {
            IList<SecurityIdentifier> currentMembers = this.GetLocalGroupMembers(groupName);
            IEnumerable<SecurityIdentifier> membersToAdd = expectedMembers.Except(currentMembers);
            IEnumerable<SecurityIdentifier> membersToRemove = allowOthers ? null : currentMembers.Except(expectedMembers);

            return this.UpdateLocalGroupMembership(groupName, membersToAdd, membersToRemove, ignoreErrors);
        }

        public bool IsDomainController()
        {
            if (this.isDC == null)
            {
                var info = NativeMethods.GetServerInfo(null);
                this.isDC = (info.Type & ServerTypes.DomainCtrl) == ServerTypes.DomainCtrl || (info.Type & ServerTypes.BackupDomainCtrl) == ServerTypes.BackupDomainCtrl;
            }

            return this.isDC.Value;
        }

        public void SetLocalAccountPassword(SecurityIdentifier sid, string password)
        {
            using (var context = new PrincipalContext(ContextType.Machine))
            {
                using (var user = UserPrincipal.FindByIdentity(context, IdentityType.Sid, sid.ToString()))
                {
                    if (user == null)
                    {
                        throw new ObjectNotFoundException("The local administrator account could not be found");
                    }

                    user.SetPassword(password);
                }
            }
        }
    }
}