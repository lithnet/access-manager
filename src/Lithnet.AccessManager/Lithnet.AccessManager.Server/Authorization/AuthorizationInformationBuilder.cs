using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class AuthorizationInformationBuilder : IAuthorizationInformationBuilder
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        private readonly AuthorizationOptions options;

        private readonly IPowerShellSecurityDescriptorGenerator powershell;

        private readonly IAuthorizationInformationMemoryCache authzCache;

        private readonly ITargetDataProvider targetDataProvider;

        private readonly IAuthorizationContextProvider authorizationContextProvider;

        public AuthorizationInformationBuilder(IOptionsSnapshot<AuthorizationOptions> options, IDirectory directory, ILogger<AuthorizationInformationBuilder> logger, IPowerShellSecurityDescriptorGenerator powershell, IAuthorizationInformationMemoryCache authzCache, ITargetDataProvider targetDataProvider, IAuthorizationContextProvider authorizationContextProvider)
        {
            this.directory = directory;
            this.logger = logger;
            this.options = options.Value;
            this.powershell = powershell;
            this.authzCache = authzCache;
            this.targetDataProvider = targetDataProvider;
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
            info = this.BuildAuthorizationInformation(user, computer);

            if (options.AuthZCacheDuration >= 0)
            {
                authzCache.Set(key, info, TimeSpan.FromSeconds(Math.Max(options.AuthZCacheDuration, 60)));
            }

            return info;
        }

        private AuthorizationInformation BuildAuthorizationInformation(IUser user, IComputer computer)
        {
            AuthorizationInformation info = new AuthorizationInformation
            {
                MatchedComputerTargets = this.GetMatchingTargetsForComputer(computer),
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
            }

            this.logger.LogTrace($"User {user.MsDsPrincipalName} has effective access of {info.EffectiveAccess} on computer {computer.MsDsPrincipalName}");

            return info;
        }

        public IList<SecurityDescriptorTarget> GetMatchingTargetsForComputer(IComputer computer)
        {
            List<SecurityDescriptorTarget> matchingTargets = new List<SecurityDescriptorTarget>();

            Lazy<List<SecurityIdentifier>> computerTokenSids = new Lazy<List<SecurityIdentifier>>(() => this.directory.GetTokenGroups(computer, computer.Sid.AccountDomainSid).ToList());
            Lazy<List<Guid>> computerParents = new Lazy<List<Guid>>(() => computer.GetParentGuids().ToList());

            foreach (var target in this.options.ComputerTargets.OrderBy(t => (int)t.Type).ThenByDescending(this.targetDataProvider.GetSortOrder))
            {
                TargetData targetData = this.targetDataProvider.GetTargetData(target);

                try
                {
                    if (target.Type == TargetType.Container)
                    {
                        if (computerParents.Value.Any(t => t == targetData.ContainerGuid))
                        {
                            this.logger.LogTrace($"Matched {computer.MsDsPrincipalName} to target OU {target.Target}");
                            matchingTargets.Add(target);
                        }
                    }
                    else if (target.Type == TargetType.Computer)
                    {
                        if (targetData.Sid == computer.Sid)
                        {
                            this.logger.LogTrace($"Matched {computer.MsDsPrincipalName} to target {target.Id}");
                            matchingTargets.Add(target);
                        }
                    }
                    else
                    {
                        if (this.directory.IsSidInPrincipalToken(targetData.Sid, computerTokenSids.Value))
                        {
                            this.logger.LogTrace($"Matched {computer.MsDsPrincipalName} to target {target.Id}");
                            matchingTargets.Add(target);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogEventError(EventIDs.TargetRuleProcessingError, $"An error occurred processing the target {target.Id}:{target.Type}:{target.Target}", ex);
                }
            }

            return matchingTargets;
        }
    }
}