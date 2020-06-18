using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class JitWorker : IJitWorker
    {
        private readonly ILogger<JitWorker> logger;

        private readonly IDirectory directory;

        private readonly IJitSettingsProvider settings;

        public JitWorker(ILogger<JitWorker> logger, IDirectory directory, IJitSettingsProvider settings)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
        }

        public void DoCheck()
        {
            IComputer computer = this.directory.GetComputer();
            IGroup group = this.GetOrCreateJitGroup(computer);

            if (group == null)
            {
                return;
            }

            this.directory.TryGetLamSettings(computer, out ILamSettings lamSettings);

            if (!this.settings.PublishLamObject)
            {
                if (lamSettings != null)
                {
                    this.directory.DeleteLamSettings(lamSettings);
                }
            }
            else if (!this.settings.PublishJitGroup)
            {
                if (!string.IsNullOrWhiteSpace(lamSettings?.JitGroupReference))
                {
                    lamSettings.UpdateJitGroup(null);
                }
            }
            else
            {
                if (lamSettings?.JitGroupReference != null)
                {
                    if (!this.directory.TryGetGroup(lamSettings.JitGroupReference, out IGroup lamGroup))
                    {
                        if (lamGroup.Sid != group.Sid)
                        {
                            lamSettings.UpdateJitGroup(group);
                        }
                    }
                }
                else
                {
                    lamSettings.UpdateJitGroup(group);
                }
            }

            this.ProcessLocalAdmins(group);
        }

        private void ProcessLocalAdmins(IGroup group)
        {
            NTAccount adminGroup = (NTAccount)new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null).Translate(typeof(NTAccount));
            string adminGroupName = adminGroup.Value.Split('\\')[1];
            List<SecurityIdentifier> allowedAdmins = this.GetOtherAllowedAdmins();
            allowedAdmins.Add(group.Sid);

            var currentMembers = this.directory.GetLocalGroupMembers(adminGroupName);
            allowedAdmins.AddRange(currentMembers.Where(t => t.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)));

            IEnumerable<SecurityIdentifier> membersToRemove = currentMembers.Except(allowedAdmins);
            IEnumerable<SecurityIdentifier> membersToAdd = allowedAdmins.Except(currentMembers);

            foreach (var member in membersToAdd)
            {
                try
                {
                    this.directory.AddLocalGroupMember(adminGroupName, member);
                    this.logger.LogInformation($"Added member to administrators group {member}");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Failed to add member to administrators group {member}");
                }
            }

            if (!this.settings.RemoveUnmanagedMembers)
            {
                return;
            }

            foreach (var member in membersToRemove)
            {
                try
                {
                    this.directory.RemoveLocalGroupMember(adminGroupName, member);
                    this.logger.LogInformation($"Removed member from administrators group {member}");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Failed to remove member from administrators group {member}");
                }
            }
        }

        private List<SecurityIdentifier> GetOtherAllowedAdmins()
        {
            List<SecurityIdentifier> allowedAdmins = new List<SecurityIdentifier>();

            foreach (string admin in this.settings.AllowedAdmins)
            {
                try
                {
                    if (admin.TryParseAsSid(out SecurityIdentifier sid))
                    {
                        allowedAdmins.Add(sid);
                    }
                    else
                    {
                        var otherAdmin = this.directory.GetPrincipal(admin);
                        allowedAdmins.Add(otherAdmin.Sid);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogTrace(ex, $"The allowed admin '{admin}' from the policy could not be resolved");
                }
            }

            return allowedAdmins;
        }

        private IGroup GetOrCreateJitGroup(IComputer computer)
        {
            IGroup group = null;

            if (!string.IsNullOrEmpty(this.settings.JitGroup))
            {
                if (!this.TryFindGroupByName(this.settings.JitGroup, out group))
                {
                    logger.LogError($"Could not find the JIT group in the directory {this.settings.JitGroup}");
                    return null;
                }
            }

            if (group == null)
            {
                if (!this.TryGetExpectedName(out string expectedName))
                {
                    logger.LogTrace("No group template was specified");
                    return null;
                }

                // Find group by name
                if (!this.TryFindGroupByName($"{this.directory.GetMachineNetbiosDomainName()}\\{expectedName}", out group))
                {
                    if (!this.settings.CreateGroup)
                    {
                        logger.LogWarning($"No JIT group was specified in group policy, and self-creation of group was not enabled");
                        return null;
                    }

                    logger.LogTrace($"Attempting to create a group named {expectedName}");
                    group = this.CreateGroup(computer, expectedName);
                }
            }

            return group;
        }

        private IGroup CreateGroup(IComputer computer, string expectedName)
        {
            string ouToCreate = this.settings.GroupCreateOu;

            DirectoryEntry ou;
            if (string.IsNullOrEmpty(ouToCreate))
            {
                DirectoryEntry computerde = computer.GetDirectoryEntry();
                ou = computerde.Parent;
            }
            else
            {
                ou = new DirectoryEntry($"LDAP://{ouToCreate}");
            }

            DirectoryEntry de = ou.Children.Add($"CN={expectedName}", "group");
            de.Properties["samAccountName"].Add(expectedName);
            de.Properties["description"].Add("JIT access group created by Lithnet Access Manager");
            de.Properties["groupType"].Add(this.settings.GroupType);
            de.CommitChanges();

            return this.directory.GetGroup(de.GetPropertySid("objectSid"));
        }

        private bool TryFindGroupByName(string name, out IGroup group)
        {
            group = null;

            try
            {
                group = this.directory.GetGroup(name);
                return true;
            }
            catch (ObjectNotFoundException)
            {
            }

            return false;
        }

        private bool TryGetExpectedName(out string expectedName)
        {
            expectedName = null;

            string groupNameTemplate = this.settings.GroupNameTemplate;

            if (groupNameTemplate == null)
            {
                return false;
            }

            expectedName = groupNameTemplate.Replace("{computerName}", Environment.MachineName, StringComparison.OrdinalIgnoreCase);
            return true;
        }

    }
}
