using System;
using System.DirectoryServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class TargetDataProvider : ITargetDataProvider
    {
        private readonly ITargetDataCache targetDataCache;

        private readonly ILogger<TargetDataProvider> logger;

        public TargetDataProvider(ITargetDataCache targetDataCache, ILogger<TargetDataProvider> logger)
        {
            this.targetDataCache = targetDataCache;
            this.logger = logger;
        }

        public TargetData GetTargetData(SecurityDescriptorTarget target)
        {
            var item = this.targetDataCache.Get<TargetData>(target.Id);

            if (item == null || item.Target != target.Target)
            {
                item = new TargetData()
                {
                    ContainerGuid = this.GetContainerGuid(target),
                    Target = target.Target,
                    Sid = this.GetSid(target),
                    SortOrder = this.GetSortOrderInternal(target)
                };
            }

            this.targetDataCache.Set(target.Id, item);

            return item;
        }

        private Guid GetContainerGuid(SecurityDescriptorTarget target)
        {
            try
            {
                if (target.Type == TargetType.AdContainer)
                {
                    DirectoryEntry de = new DirectoryEntry($"LDAP://{target.Target}");
                    return de.Guid;
                }
                else
                {
                    return Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.TargetDirectoryLookupError, ex, $"Could not find GUID for target {target.Target}");
                return Guid.Empty;
            }
        }

        private SecurityIdentifier GetSid(SecurityDescriptorTarget target)
        {
            if (target.Type == TargetType.AdContainer || target.Type == TargetType.AadTenant)
            {
                return null;
            }

            if (target.Target == null)
            {
                throw new ArgumentNullException(nameof(target.Target), "The target was null");
            }

            return new SecurityIdentifier(target.Target);
        }

        public int GetSortOrder(SecurityDescriptorTarget target)
        {
            return this.GetTargetData(target).SortOrder;
        }

        private int GetSortOrderInternal(SecurityDescriptorTarget target)
        {
            try
            {
                if (target.Type == TargetType.AdContainer && !string.IsNullOrWhiteSpace(target.Target))
                {
                    X500DistinguishedName x500 = new X500DistinguishedName(target.Target);
                    return x500.Decode(X500DistinguishedNameFlags.UseNewLines)?.Split("\r\n")?.Length ?? 0;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.DNParseError, ex, $"Unable to parse DN {target.Target}. Using default sort order of 0");
            }

            return 0;
        }
    }
}
