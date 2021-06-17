using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class AuthorizationInformationBuilder : IAuthorizationInformationBuilder
    {
        private readonly ILogger logger;
        private readonly AuthorizationOptions options;
        private readonly IPowerShellSecurityDescriptorGenerator powershell;
        private readonly IAuthorizationInformationMemoryCache authzCache;
        private readonly IEnumerable<IComputerTargetProvider> computerTargetProviders;
        private readonly IAuthorizationContextProvider authorizationContextProvider;
        private readonly IAmsLicenseManager licenseManager;

        public AuthorizationInformationBuilder(IOptionsSnapshot<AuthorizationOptions> options, ILogger<AuthorizationInformationBuilder> logger, IPowerShellSecurityDescriptorGenerator powershell, IAuthorizationInformationMemoryCache authzCache, IEnumerable<IComputerTargetProvider> computerTargetProviders, IAuthorizationContextProvider authorizationContextProvider, IAmsLicenseManager licenseManager)
        {
            this.logger = logger;
            this.options = options.Value;
            this.powershell = powershell;
            this.authzCache = authzCache;
            this.computerTargetProviders = computerTargetProviders;
            this.authorizationContextProvider = authorizationContextProvider;
            this.licenseManager = licenseManager;
        }

        public void ClearCache(IUser user, IComputer computer)
        {
            string key = $"{user.Sid}-{computer.Authority}-{computer.AuthorityDeviceId}-{computer.AuthorityType}";
            authzCache.Remove(key);
        }

        public async Task<AuthorizationInformation> GetAuthorizationInformation(IUser user, IComputer computer)
        {
            string key = $"{user.Sid}-{computer.Authority}-{computer.AuthorityDeviceId}-{computer.AuthorityType}";

            if (authzCache.TryGetValue(key, out AuthorizationInformation info))
            {
                this.logger.LogTrace($"Cached authorization information found for {key}");
                return info;
            }

            this.logger.LogTrace($"Building authorization information for {key}");

            List<SecurityDescriptorTarget> targets = new List<SecurityDescriptorTarget>();

            foreach (var computerTargetProvider in this.computerTargetProviders)
            {
                if (computerTargetProvider.CanProcess(computer))
                {
                    targets.AddRange(await computerTargetProvider.GetMatchingTargetsForComputer(computer, this.options.ComputerTargets));
                }
            }

            info = await this.BuildAuthorizationInformation(user, computer, targets);

            if (options.AuthZCacheDuration >= 0)
            {
                authzCache.Set(key, info, TimeSpan.FromSeconds(Math.Max(options.AuthZCacheDuration, 60)));
            }

            return info;
        }

        public Task<AuthorizationInformation> BuildAuthorizationInformation(IUser user, IComputer computer, IList<SecurityDescriptorTarget> matchedComputerTargets)
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
                return Task.FromResult(info);
            }

            AuthorizationContext c;

            if (computer is IActiveDirectoryComputer adComputer)
            {
                c = authorizationContextProvider.GetAuthorizationContext(user, adComputer.Sid);
            }
            else
            {
                c = authorizationContextProvider.GetAuthorizationContext(user);
            }

            using (c)
            {

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
                        if (!this.licenseManager.IsFeatureEnabled(LicensedFeatures.PowerShellAcl))
                        {
                            continue;
                        }

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
                        AccessMask mask = (AccessMask)ace.AccessMask;

                        if (mask.HasFlag(AccessMask.LocalAdminPasswordHistory) && ace.AceType == AceType.AccessAllowed)
                        {
                            if (!this.licenseManager.IsFeatureEnabled(LicensedFeatures.LapsHistory))
                            {
                                mask &= ~AccessMask.LocalAdminPasswordHistory;
                            }
                        }

                        if (mask.HasFlag(AccessMask.Jit) && (!(computer is IActiveDirectoryComputer)))
                        {
                            mask &= ~AccessMask.Jit;
                        }

                        if (mask.HasFlag(AccessMask.BitLocker) && (!(computer is IActiveDirectoryComputer)))
                        {
                            mask &= ~AccessMask.BitLocker;
                        }

                        if (mask != 0)
                        {
                            masterDacl.AddAccess((AccessControlType)ace.AceType, ace.SecurityIdentifier, (int)mask, ace.InheritanceFlags, ace.PropagationFlags);
                        }
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

                    this.logger.LogTrace($"Resultant security descriptor for computer {computer.FullyQualifiedName}: {info.SecurityDescriptor.GetSddlForm(AccessControlSections.All)}");

                    info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptor, (int)AccessMask.LocalAdminPassword) ? AccessMask.LocalAdminPassword : 0;
                    info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptor, (int)AccessMask.Jit) ? AccessMask.Jit : 0;
                    info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptor, (int)AccessMask.LocalAdminPasswordHistory) ? AccessMask.LocalAdminPasswordHistory : 0;
                    info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptor, (int)AccessMask.BitLocker) ? AccessMask.BitLocker : 0;
                }

                this.logger.LogTrace($"User {user.MsDsPrincipalName} has effective access of {info.EffectiveAccess} on computer {computer.FullyQualifiedName}");

                return Task.FromResult(info);
            }
        }
    }
}