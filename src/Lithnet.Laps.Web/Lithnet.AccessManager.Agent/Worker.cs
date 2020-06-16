using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;

        private readonly IDirectory directory;

        private readonly ISettingsProvider settings;

        private readonly IHostApplicationLifetime appLifetime;

        public Worker(ILogger<Worker> logger, IDirectory directory, ISettingsProvider settings, IHostApplicationLifetime appLifetime)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.appLifetime = appLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogTrace("Worker running at: {time}", DateTimeOffset.Now);

                if (this.directory.IsDomainController())
                {
                    this.logger.LogWarning("This application should not be run on a domain controller. Shutting down");
                    this.appLifetime.StopApplication();
                    return;
                }

                this.RunCheck();

                await Task.Delay(TimeSpan.FromMinutes(Math.Max(this.settings.CheckInterval, 5)), stoppingToken);
            }
        }

        private void RunCheck()
        {
            try
            {
                if (!this.settings.Enabled)
                {
                    logger.LogTrace("Lithnet Access Manager agent is not enabled");
                    return;
                }

                IComputer computer = this.directory.GetComputer();

                IGroup group = this.GetOrCreateGroup(computer);

                if (group == null)
                {
                    return;
                }

                ILamSettings lamSettings = this.TryGetLamSetings();

                if (!this.settings.PublishLamObject)
                {
                    if (lamSettings != null)
                    {
                        this.directory.UpdateLamSettings(computer, null, null);
                    }
                }
                else
                {
                    if (lamSettings != null)
                    {
                        if (!this.TryGetLamSettingsGroup(lamSettings, out IGroup lamGroup))
                        {
                            if (lamGroup.Sid != group.Sid)
                            {
                                this.directory.UpdateLamSettings(computer, group);
                            }
                        }
                    }
                    else
                    {
                        this.directory.UpdateLamSettings(computer, group);
                    }
                }

                this.ProcessLocalAdmins(group);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An unexpected error occurred");
            }
        }

        private void ProcessLocalAdmins(IGroup group)
        {
            NTAccount adminGroup = (NTAccount)new SecurityIdentifier("S-1-5-32-544").Translate(typeof(NTAccount));
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

        private IGroup GetOrCreateGroup(IComputer computer)
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

        private bool TryGetLamSettingsGroup(ILamSettings settings, out IGroup group)
        {
            group = null;

            try
            {
                group = this.directory.GetGroup(settings.MsDsObjectReference);
                return true;
            }
            catch (ObjectNotFoundException)
            {
            }

            return false;
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

        private bool TryFindGroupBySid(SecurityIdentifier sid, out IGroup group)
        {
            group = null;

            try
            {
                group = this.directory.GetGroup(sid);
            }
            catch (ObjectNotFoundException)
            {
            }

            return false;
        }

        private bool TryGetExistingSid(out SecurityIdentifier sid)
        {
            string groupSIDString = this.settings.CachedGroupSid;
            sid = null;

            if (groupSIDString != null)
            {
                groupSIDString.TryParseAsSid(out sid);
                this.logger.LogTrace($"Found existing group SID {sid}");
                return true;
            }

            return false;
        }

        private ILamSettings TryGetLamSetings()
        {
            try
            {
                var settings = this.directory.GetLamSettings(this.directory.GetComputer());
                this.logger.LogTrace($"Found existing LAM settings object");
            }
            catch (ObjectNotFoundException)
            {
            }

            return null;
        }
    }
}