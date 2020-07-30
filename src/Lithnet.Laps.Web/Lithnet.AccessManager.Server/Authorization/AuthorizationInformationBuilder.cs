using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
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

        private readonly BuiltInProviderOptions options;

        private readonly IPowerShellSecurityDescriptorGenerator powershell;

        private readonly IAuthorizationInformationMemoryCache cache;

        public AuthorizationInformationBuilder(IOptionsSnapshot<BuiltInProviderOptions> options, IDirectory directory, ILogger<AuthorizationInformationBuilder> logger, IPowerShellSecurityDescriptorGenerator powershell, IAuthorizationInformationMemoryCache cache)
        {
            this.directory = directory;
            this.logger = logger;
            this.options = options.Value;
            this.powershell = powershell;
            this.cache = cache;
        }

        public void ClearCache(IUser user, IComputer computer)
        {
            string key = $"{user.Sid}-{computer.Sid}";
            cache.Remove(key);
        }

        public AuthorizationInformation GetAuthorizationInformation(IUser user, IComputer computer)
        {
            string key = $"{user.Sid}-{computer.Sid}";

            if (cache.TryGetValue(key, out AuthorizationInformation info))
            {
                this.logger.LogTrace($"Cached authorization information found for {key}");
                return info;
            }

            this.logger.LogTrace($"Building authorization information for {key}");
            info = this.BuildAuthorizationInformation(user, computer);

            if (options.AuthZCacheDuration >= 0)
            {
                cache.Set(key, info, TimeSpan.FromSeconds(Math.Max(options.AuthZCacheDuration, 60)));
            }

            return info;
        }

        private AuthorizationInformation BuildAuthorizationInformation(IUser user, IComputer computer)
        {
            AuthorizationInformation info = new AuthorizationInformation
            {
                MatchedTargets = this.GetMatchingTargetsForComputer(computer)
            };

            if (info.MatchedTargets.Count == 0)
            {
                return info;
            }

            AuthorizationContext c = new AuthorizationContext(user.Sid, this.GetAuthorizationContextTarget(user, computer));

            foreach (var target in info.MatchedTargets)
            {
                GenericSecurityDescriptor sd;

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

                    sd = new RawSecurityDescriptor(target.SecurityDescriptor);
                }

                if (sd == null)
                {
                    this.logger.LogTrace($"Ignoring target {target.Id} with null security descriptor");
                    continue;
                }

                info.SecurityDescriptors.Add(sd);

                bool hasLaps = c.AccessCheck(sd, (int)AccessMask.Laps);
                bool hasLapsHistory = c.AccessCheck(sd, (int)AccessMask.LapsHistory);
                bool hasJit = c.AccessCheck(sd, (int)AccessMask.Jit);

                if (hasJit)
                {
                    info.SuccessfulJitTargets.Add(target);
                }

                if (hasLaps)
                {
                    info.SuccessfulLapsTargets.Add(target);
                }

                if (hasLapsHistory)
                {
                    info.SuccessfulLapsHistoryTargets.Add(target);
                }

                // If the ACE did not grant any permissions to the user, consider it a failure response
                if (!hasLaps &&
                    !hasLapsHistory &&
                    !hasJit)
                {
                    info.FailedTargets.Add(target);
                }
            }

            info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptors, (int)AccessMask.Laps) ? AccessMask.Laps : 0;
            info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptors, (int)AccessMask.Jit) ? AccessMask.Jit : 0;
            info.EffectiveAccess |= c.AccessCheck(info.SecurityDescriptors, (int)AccessMask.LapsHistory) ? AccessMask.LapsHistory : 0;

            this.logger.LogTrace($"User {user.MsDsPrincipalName} has effective access of {info.EffectiveAccess} on computer {computer.MsDsPrincipalName}");
            return info;
        }

        private string GetAuthorizationContextTarget(IUser user, IComputer computer)
        {
            switch (this.options.AccessControlEvaluationLocation)
            {
                case AclEvaluationLocation.ComputerDomain:
                    return this.directory.GetDomainNameDnsFromSid(computer.Sid);

                case AclEvaluationLocation.UserDomain:
                    return this.directory.GetDomainNameDnsFromSid(user.Sid);

                default:
                    return null;
            }
        }

        public IList<SecurityDescriptorTarget> GetMatchingTargetsForComputer(IComputer computer)
        {
            List<SecurityDescriptorTarget> matchingTargets = new List<SecurityDescriptorTarget>();

            foreach (var target in this.options.Targets.OrderBy(t => (int)t.Type).ThenByDescending(t => t.Id.Length))
            {
                try
                {
                    if (target.Type == TargetType.Container)
                    {
                        if (this.directory.IsObjectInOu(computer, target.Target))
                        {
                            this.logger.LogTrace($"Matched {computer.MsDsPrincipalName} to target OU {target.Target}");
                            matchingTargets.Add(target);
                        }
                    }
                    else if (target.Type == TargetType.Computer)
                    {
                        if (target.GetTargetAsSid() == computer.Sid)
                        {
                            this.logger.LogTrace($"Matched {computer.MsDsPrincipalName} to target {target.Id}");
                            matchingTargets.Add(target);
                        }
                    }
                    else
                    {
                        if (this.directory.IsSidInPrincipalToken(target.GetTargetAsSid(), computer, computer.Sid))
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
