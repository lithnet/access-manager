using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class JitAgent : IJitAgent
    {
        private readonly ILogger<JitAgent> logger;

        private readonly IDirectory directory;

        private readonly IJitSettings settings;

        private readonly ILocalSam sam;

        private readonly IAppDataProvider appDataProvider;

        public JitAgent(ILogger<JitAgent> logger, IDirectory directory, IJitSettings settings, ILocalSam sam, IAppDataProvider provider)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.sam = sam;
            this.appDataProvider = provider;
        }

        public void DoCheck()
        {
            try
            {
                if (!this.settings.JitEnabled)
                {
                    return;
                }

                IComputer computer = this.directory.GetComputer(this.sam.GetMachineNTAccountName());

                IGroup group = this.GetOrCreateJitGroup(computer);

                this.sam.UpdateLocalGroupMembership(
                    this.sam.GetBuiltInAdministratorsGroupName(),
                    this.BuildExpectedMembership(group.Sid),
                    this.settings.AllowUnmanagedAdmins,
                    true);

                this.UpdateJitGroupRegistration(computer, group);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The JIT agent process encountered an error");
            }
        }

        internal void UpdateJitGroupRegistration(IComputer computer, IGroup jitGroup)
        {
            this.appDataProvider.TryGetAppData(computer, out IAppData appData);

            // If we've been asked to not publish a JIT group and there is one published
            if (!this.settings.PublishJitGroup && !string.IsNullOrWhiteSpace(appData?.JitGroupReference))
            {
                appData.ClearJitGroup();
                this.logger.LogInformation("Unpublished JIT group information from the directory");
                return;
            }

            if (appData == null)
            {
                this.logger.LogTrace("Existing settings object not found");
                appData = this.appDataProvider.Create(computer);
                this.logger.LogInformation("Created settings object in directory as {distinguishedName}", appData.DistinguishedName);
            }

            if (appData.JitGroupReference == null || !DirectoryExtensions.IsDnMatch(appData.JitGroupReference, jitGroup.DistinguishedName))
            {
                this.logger.LogTrace("JIT group update required. Expected {expected}. Actual {actual}", jitGroup.DistinguishedName, appData.JitGroupReference);
                appData.UpdateJitGroup(jitGroup);
                this.logger.LogInformation("Update JIT reference in the directory to {distinguishedName}", jitGroup.DistinguishedName);
            }
        }

        internal List<SecurityIdentifier> BuildExpectedMembership(SecurityIdentifier jitGroupSid)
        {
            List<SecurityIdentifier> allowedAdmins = this.GetOtherAllowedAdmins();
            allowedAdmins.Add(jitGroupSid);
            allowedAdmins.Add(this.sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid));
            return allowedAdmins;
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
            IGroup group;

            if (!this.TryGetGroupName(out string name))
            {
                throw new ConfigurationException("No JIT group was specified in the configuration");
            }

            if (this.directory.TryGetGroup(name, out group))
            {
                return group;
            }

            if (name.TryParseAsSid(out _))
            {
                throw new ObjectNotFoundException($"The JIT group could not be found: {name}");
            }

            if (!this.settings.CreateJitGroup)
            {
                throw new ConfigurationException("No JIT group was specified in group policy, and self-creation of group was not enabled");
            }

            logger.LogTrace($"Attempting to create a group named {name}");
            return this.directory.CreateGroup(name, this.settings.JitGroupDescription, this.settings.JitGroupType, this.DetermineCreationOU(computer));
        }

        private DirectoryEntry DetermineCreationOU(IComputer computer)
        {
            string ouToCreate = this.settings.JitGroupCreationOU;

            DirectoryEntry ou;
            if (string.IsNullOrEmpty(ouToCreate))
            {
                ou = computer.GetParentDirectoryEntry();
            }
            else
            {
                ou = new DirectoryEntry($"LDAP://{ouToCreate}");
            }

            return ou;
        }

        private bool TryGetGroupName(out string groupName)
        {
            groupName = this.settings.JitGroup;

            if (groupName == null)
            {
                return false;
            }

            string domain = this.sam.GetMachineNetbiosDomainName();
            groupName = groupName
                .Replace("{computerName}", Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                .Replace("{domain}", domain, StringComparison.OrdinalIgnoreCase);

            if (!groupName.Contains('\\') && !groupName.TryParseAsSid(out _))
            {
                groupName = $"{domain}\\{groupName}";
            }

            return true;
        }
    }
}
