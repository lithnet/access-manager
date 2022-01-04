using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class RoleAuthorizationInformationBuilder
    {
        private readonly ILogger logger;
        private readonly AuthorizationOptions options;
        private readonly IPowerShellSecurityDescriptorGenerator powershell;
        private readonly IAuthorizationInformationMemoryCache authzCache;
        private readonly IAuthorizationContextProvider authorizationContextProvider;
        private readonly IAmsLicenseManager licenseManager;

        public RoleAuthorizationInformationBuilder(IOptionsSnapshot<AuthorizationOptions> options, ILogger<AuthorizationInformationBuilder> logger, IPowerShellSecurityDescriptorGenerator powershell, IAuthorizationInformationMemoryCache authzCache, IAuthorizationContextProvider authorizationContextProvider, IAmsLicenseManager licenseManager)
        {
            this.logger = logger;
            this.options = options.Value;
            this.powershell = powershell;
            this.authzCache = authzCache;
            this.authorizationContextProvider = authorizationContextProvider;
            this.licenseManager = licenseManager;
        }

        public void ClearCache(IActiveDirectoryUser user)
        {
            string key = $"roles-{user.Sid}";
            authzCache.Remove(key);
        }

        public async Task<RoleAuthorizationInformation> GetAuthorizationInformation(IActiveDirectoryUser user)
        {
            string key = $"roles-{user.Sid}";

            if (authzCache.TryGetValue(key, out RoleAuthorizationInformation info))
            {
                this.logger.LogTrace($"Cached authorization information found for {key}");
                return info;
            }

            this.logger.LogTrace($"Building authorization information for {key}");

            info = await this.BuildAuthorizationInformation(user);

            if (options.AuthZCacheDuration >= 0)
            {
                authzCache.Set(key, info, TimeSpan.FromSeconds(Math.Max(options.AuthZCacheDuration, 60)));
            }

            return info;
        }

        public Task<RoleAuthorizationInformation> BuildAuthorizationInformation(IActiveDirectoryUser user)
        {
            RoleAuthorizationInformation info = new RoleAuthorizationInformation
            {
                MatchedTargets = new List<RoleSecurityDescriptorTarget>(),
                User = user
            };

            AuthorizationContext authzContext = this.authorizationContextProvider.GetAuthorizationContext(user);

            using (authzContext)
            {
                foreach (var target in options.Roles)
                {
                    if (this.IsRoleAuthorized(user, target, authzContext))
                    {
                        info.MatchedTargets.Add(target);

                    }
                }

                this.logger.LogTrace($"Found {info.MatchedTargets.Count} roles for user {user.MsDsPrincipalName}");

                return Task.FromResult(info);
            }
        }

        public bool IsRoleAuthorized(IActiveDirectoryUser user, RoleSecurityDescriptorTarget target)
        {
            AuthorizationContext authzContext = this.authorizationContextProvider.GetAuthorizationContext(user);
            return this.IsRoleAuthorized(user, target, authzContext);
        }

        private bool IsRoleAuthorized(IActiveDirectoryUser user, RoleSecurityDescriptorTarget target, AuthorizationContext authzContext)
        {
            CommonSecurityDescriptor sd;

            if (target.IsInactive())
            {
                return false;
            }

            if (target.AuthorizationMode == AuthorizationMode.PowershellScript)
            {
                if (!this.licenseManager.IsFeatureEnabled(LicensedFeatures.PowerShellAcl))
                {
                    return false;
                }

                //TODO: Add overload for role based access evaluation
                sd = this.powershell.GenerateSecurityDescriptor(user, null, target.Script, 30);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(target.SecurityDescriptor))
                {
                    this.logger.LogTrace($"Ignoring target {target.Id} with empty security descriptor");
                    return false;
                }

                sd = new CommonSecurityDescriptor(false, false, new RawSecurityDescriptor(target.SecurityDescriptor));
            }

            if (sd == null)
            {
                this.logger.LogTrace($"Ignoring target {target.Id} with null security descriptor");
                return false;
            }

            return (authzContext.AccessCheck(sd, (int)AccessMask.Jit));
        }
    }
}