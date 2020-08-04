using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class AuthorizationContextProvider : IAuthorizationContextProvider
    {
        private readonly IDirectory directory;
        private readonly ILogger<AuthorizationContextProvider> logger;
        private readonly ConcurrentDictionary<SecurityIdentifier, AuthorizationContextDomainDetails> domainCache;
        private readonly AuthorizationOptions options;

        public AuthorizationContextProvider(IOptions<AuthorizationOptions> options, IDirectory directory, ILogger<AuthorizationContextProvider> logger)
        {
            this.directory = directory;
            this.logger = logger;
            this.options = options.Value;
            this.domainCache = new ConcurrentDictionary<SecurityIdentifier, AuthorizationContextDomainDetails>();
        }

        public AuthorizationContext GetAuthorizationContext(IUser user)
        {
            return this.GetAuthorizationContext(user, user.Sid.AccountDomainSid);
        }

        public AuthorizationContext GetAuthorizationContext(IUser user, SecurityIdentifier resourceDomain)
        {
            var domainDetails = this.GetAuthorizationContextDomainDetails(user.Sid, resourceDomain.AccountDomainSid);

            try
            {
                return this.GetContext(user, resourceDomain.AccountDomainSid, domainDetails);
            }
            catch
            {
                // If we only have a one-way trust to the remote domain, don't bother trying to fallback because it will fail locally
                // as that user can't log on in this domain at all

                if (!domainDetails.IsRemoteOneWayTrust && !domainDetails.Mapping.DisableLocalFallback)
                {
                    logger.LogWarning("Unable to establish authorization context for user {user} against an appropriate target server. The authorization context will be built locally, but information about membership in domain local groups in the target domain may missed", user.MsDsPrincipalName);
                    return new AuthorizationContext(user.Sid);
                }
                else
                {
                    throw;
                }
            }
        }

        public AuthorizationContext GetContext(IUser user, SecurityIdentifier resourceDomain, AuthorizationContextDomainDetails domainDetails)
        {
            AuthorizationServer server = domainDetails.GetServer(false);

            Exception lastException = null;
            HashSet<string> attemptedServers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (attemptedServers.Add(server.Name))
            {
                try
                {
                    this.logger.LogTrace("Attempting to create AuthorizationContext against server {server} in domain {domain} for user {user} requesting access to resource in domain {domain} ", server.Name, domainDetails.DomainDnsName, user.MsDsPrincipalName, resourceDomain);
                    return new AuthorizationContext(user.Sid, server.Name, domainDetails.Mapping.DoNotRequireS4U ? AuthzInitFlags.Default : AuthzInitFlags.RequireS4ULogon);
                }
                catch (AuthorizationContextException ex) when (ex.InnerException is Win32Exception we && we.HResult == -2147467259) //RPC_NOT_AVAILABLE
                {
                    lastException = ex;
                    this.logger.LogWarning(ex, "Unable to connect to server {server}", server.Name);
                    server = domainDetails.GetServer(true);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    this.logger.LogError(ex, "Unable to create AuthorizationContext against server {server} in domain {domain}", server.Name, domainDetails.DomainDnsName);
                }
            }

            throw lastException ?? new Exception("Unable to create authorization context");
        }

        public AuthorizationContextDomainDetails GetAuthorizationContextDomainDetails(SecurityIdentifier userSid, SecurityIdentifier resourceDomainSid)
        {
            var userDomain = this.GetDomainDetails(userSid);
            var computerDomain = this.GetDomainDetails(resourceDomainSid);

            if (userDomain.IsInCurrentForest && computerDomain.IsRemoteOneWayTrust)
            {
                return userDomain;
            }
            else
            {
                return computerDomain;
            }
        }

        private AuthorizationContextDomainDetails GetDomainDetails(SecurityIdentifier sid)
        {
            if (!domainCache.TryGetValue(sid.AccountDomainSid, out AuthorizationContextDomainDetails value))
            {
                string domainDnsName = this.directory.GetDomainNameDnsFromSid(sid);
                value = new AuthorizationContextDomainDetails(sid.AccountDomainSid, domainDnsName, this.directory)
                {
                    Mapping = this.GetMapping(domainDnsName)
                };

                this.domainCache.TryAdd(sid.AccountDomainSid, value);
                this.logger.LogTrace($"Built AuthorizationContextDomainDetails for domain {domainDnsName}. IsInCurrentForest:{value.IsInCurrentForest} IsRemoteOneWayTrust:{value.IsRemoteOneWayTrust}");
            }

            return value;
        }

        public AuthorizationServerMapping GetMapping(string dnsDomain)
        {
            if (this.options?.AuthorizationServerMapping != null)
            {
                foreach (var mapping in this.options.AuthorizationServerMapping)
                {
                    if (string.Equals(mapping.Domain, dnsDomain))
                    {
                        return mapping;
                    }
                }
            }

            return new AuthorizationServerMapping { Domain = dnsDomain };
        }

    }
}
