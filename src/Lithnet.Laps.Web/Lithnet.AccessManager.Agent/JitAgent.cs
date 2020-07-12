using System;
using System.Collections.Generic;
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

        public JitAgent(ILogger<JitAgent> logger, IDirectory directory, IJitSettings settings, ILocalSam sam)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.sam = sam;
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

                IGroup group = this.GetJitGroup();

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

        internal List<SecurityIdentifier> GetOtherAllowedAdmins()
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

        internal IGroup GetJitGroup()
        {
            if (!this.TryGetGroupName(out string name))
            {
                throw new ConfigurationException("No JIT group was specified in the configuration");
            }

            this.logger.LogTrace(EventIDs.JitGroupSearching, $"Searching for JIT group {name}", name);

            if (name.TryParseAsSid(out SecurityIdentifier sid))
            {
                return this.directory.GetGroup(sid);
            }

            if (this.directory.TryGetGroup(name, out IGroup group))
            {
                return group;
            }
            else
            {
                throw new ObjectNotFoundException($"The JIT group could not be found: {name}");
            }
        }

        internal bool TryGetGroupName(out string groupName)
        {
            groupName = this.settings.JitGroup;

            if (groupName == null)
            {
                return false;
            }

            string domain = this.sam.GetMachineNetbiosDomainName();

            groupName = groupName
                .Replace("{computerName}", Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                .Replace("{domain}", domain, StringComparison.OrdinalIgnoreCase)
                .Replace("{computerDomain}", domain, StringComparison.OrdinalIgnoreCase);

            if (!groupName.Contains('\\') && !groupName.TryParseAsSid(out _))
            {
                groupName = $"{domain}\\{groupName}";
            }

            return true;
        }
    }
}
