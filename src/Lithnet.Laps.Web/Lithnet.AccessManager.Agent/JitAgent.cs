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
                    this.logger.LogTrace(EventIDs.JitAgentDisabled, "The JIT agent is disabled");
                    return;
                }

                IComputer computer = this.directory.GetComputer(this.sam.GetMachineNTAccountName());

                IGroup group = this.GetOrCreateJitGroup(computer);

                this.logger.LogTrace(EventIDs.JitGroupFound, "The JIT group was found in the directory as {principalName}", group.MsDsPrincipalName);

                this.sam.UpdateLocalGroupMembership(
                    this.sam.GetBuiltInAdministratorsGroupName(),
                    this.BuildExpectedMembership(group.Sid),
                    !this.settings.RestrictAdmins,
                    true);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.JitUnexpectedException, ex, "The JIT agent process encountered an error");
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
            if (!this.TryGetGroupName(out string name))
            {
                throw new ConfigurationException("No JIT group was specified in the configuration");
            }

            this.logger.LogTrace(EventIDs.JitGroupSearching, "Searching for JIT group {name}", name);

            if (this.directory.TryGetGroup(name, out IGroup group))
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

            logger.LogTrace(EventIDs.JitGroupCreating, "Attempting to create a group named {name}", name);
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
