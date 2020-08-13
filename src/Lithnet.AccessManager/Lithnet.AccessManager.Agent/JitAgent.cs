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

        private readonly IJitAccessGroupResolver jitGroupResolver;

        private bool isDisabledLogged;

        public JitAgent(ILogger<JitAgent> logger, IDirectory directory, IJitSettings settings, ILocalSam sam, IJitAccessGroupResolver jitGroupResolver)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
            this.sam = sam;
            this.jitGroupResolver = jitGroupResolver;
        }

        public void DoCheck()
        {
            try
            {
                if (!this.settings.JitEnabled)
                {
                    if (!this.isDisabledLogged)
                    {
                        this.logger.LogTrace(EventIDs.JitAgentDisabled, "The JIT agent is disabled");
                        this.isDisabledLogged = true;
                    }

                    return;
                }

                if (this.isDisabledLogged)
                {
                    this.logger.LogTrace(EventIDs.JitAgentEnabled, "The JIT agent has been enabled");
                    this.isDisabledLogged = false;
                }

                IGroup group = this.jitGroupResolver.GetJitGroup(this.settings.JitGroup, Environment.MachineName, this.sam.GetMachineNetbiosDomainName());

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
    }
}
