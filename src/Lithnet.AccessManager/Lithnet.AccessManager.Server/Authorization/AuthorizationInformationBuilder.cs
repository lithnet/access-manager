using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class AuthorizationInformationBuilder : IAuthorizationInformationBuilder
    {
        private readonly ILogger logger;
        private readonly AuthorizationOptions options;
        private readonly IPowerShellSecurityDescriptorGenerator powershell;
        private readonly IAuthorizationInformationMemoryCache authzCache;
        private readonly IComputerTargetProvider computerTargetProvider;
        private readonly IAuthorizationContextProvider authorizationContextProvider;

        public AuthorizationInformationBuilder(IOptionsSnapshot<AuthorizationOptions> options, ILogger<AuthorizationInformationBuilder> logger, IPowerShellSecurityDescriptorGenerator powershell, IAuthorizationInformationMemoryCache authzCache, IComputerTargetProvider computerTargetProvider, IAuthorizationContextProvider authorizationContextProvider)
        {
            this.logger = logger;
            this.options = options.Value;
            this.powershell = powershell;
            this.authzCache = authzCache;
            this.computerTargetProvider = computerTargetProvider;
            this.authorizationContextProvider = authorizationContextProvider;
        }

        public void ClearCache(IUser user, IComputer computer)
        {
            string key = $"{user.Sid}-{computer.Sid}";
            authzCache.Remove(key);
        }

        public AuthorizationInformation GetAuthorizationInformation(IUser user, IComputer computer)
        {
            string key = $"{user.Sid}-{computer.Sid}";

            if (authzCache.TryGetValue(key, out AuthorizationInformation info))
            {
                this.logger.LogTrace($"Cached authorization information found for {key}");
                return info;
            }

            this.logger.LogTrace($"Building authorization information for {key}");
            var targets = this.computerTargetProvider.GetMatchingTargetsForComputer(computer, this.options.ComputerTargets);
            info = this.BuildAuthorizationInformation(user, computer, targets);

            if (options.AuthZCacheDuration >= 0)
            {
                authzCache.Set(key, info, TimeSpan.FromSeconds(Math.Max(options.AuthZCacheDuration, 60)));
            }

            return info;
        }

        public AuthorizationInformation BuildAuthorizationInformation(IUser user, IComputer computer, IList<SecurityDescriptorTarget> matchedComputerTargets)
        {
            AuthorizationInformation info = new AuthorizationInformation
            {
                MatchedComputerTargets = matchedComputerTargets,
                EffectiveAccess = 0,
                Computer = computer,
                User = user
            };

            if (info.MatchedComputerTargets.Count == 0)
            {
                return info;
            }

            using AuthorizationContext c = authorizationContextProvider.GetAuthorizationContext(user, computer.Sid);

            DiscretionaryAcl masterDacl = new DiscretionaryAcl(false, false, info.MatchedComputerTargets.Count);

            int matchedTargetCount = 0;

            foreach (var target in info.MatchedComputerTargets)
            {
                CommonSecurityDescriptor sd;

                if (target.IsInactive())
                {
                    continue;
                }

                if (target.AuthorizationMode == AuthorizationMode.PowershellScript)
                {
                    sd = this.powershell.GenerateSecurityDescriptor(user, computer, target.Script, 30);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(target.SecurityDescriptor))
                    {
                        this.logger.LogTrace($"Ignoring target {target.Id} with empty security descriptor");
                        continue;
                    }

                    sd = new CommonSecurityDescriptor(false, false, new RawSecurityDescriptor(target.SecurityDescriptor));
                }

                if (sd == null)
                {
                    this.logger.LogTrace($"Ignoring target {target.Id} with null security descriptor");
                    continue;
                }

                foreach (var ace in sd.DiscretionaryAcl.OfType<CommonAce>())
                {
                    masterDacl.AddAccess(
                        (AccessControlType)ace.AceType,
                        ace.SecurityIdentifier,
                        ace.AccessMask,
                        ace.InheritanceFlags,
                        ace.PropagationFlags);
                }

                int i = matchedTargetCount;

                if (c.AccessCheck(sd, (int)AccessMask.LocalAdminPassword))
                {
                    info.SuccessfulLapsTargets.Add(target);
                    matchedTargetCount++;
                }

                if (c.AccessCheck(sd, (int)AccessMask.LocalAdminPasswordHistory))
                {
                    info.SuccessfulLapsHistoryTargets.Add(target);
                    matchedTargetCount++;
                }

                if (c.AccessCheck(sd, (int)AccessMask.Jit))
                {
                    info.SuccessfulJitTargets.Add(target);
                    matchedTargetCount++;
                }

                if (c.AccessCheck(sd, (int)AccessMask.BitLocker))
                {
                    info.SuccessfulBitLockerTargets.Add(target);
                    matchedTargetCount++;
                }

                // If the ACE did not grant any permissions to the user, consider it a failure response
                if (i == matchedTargetCount)
                {
                    info.FailedTargets.Add(target);
                }
            }

            if (matchedTargetCount > 0)
            {
                info.SecurityDescriptor = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, masterDacl);

                this.logger.LogTrace($"Resultant security descriptor for computer {computer.MsDsPrincipalName}: {info.SecurityDescriptor.GetSddlForm(AccessControlSections.All)}");

                info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptor, (int)AccessMask.LocalAdminPassword) ? AccessMask.LocalAdminPassword : 0;
                info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptor, (int)AccessMask.Jit) ? AccessMask.Jit : 0;
                info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptor, (int)AccessMask.LocalAdminPasswordHistory) ? AccessMask.LocalAdminPasswordHistory : 0;
                info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptor, (int)AccessMask.BitLocker) ? AccessMask.BitLocker : 0;
            }

            this.logger.LogTrace($"User {user.MsDsPrincipalName} has effective access of {info.EffectiveAccess} on computer {computer.MsDsPrincipalName}");

            return info;
        }
    }
}