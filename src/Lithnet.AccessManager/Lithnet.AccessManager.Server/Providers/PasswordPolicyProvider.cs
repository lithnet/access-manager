using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace Lithnet.AccessManager.Server
{
    public class PasswordPolicyProvider : IPasswordPolicyProvider
    {
        private readonly IOptionsMonitor<PasswordPolicyOptions> policy;
        private readonly ILogger<PasswordPolicyProvider> logger;
        private readonly IPasswordPolicyMemoryCache cache;
        private readonly IDeviceProvider deviceProvider;
        private readonly IAadGraphApiProvider graphProvider;
        private readonly IAmsGroupProvider groupProvider;

        public PasswordPolicyProvider(IOptionsMonitor<PasswordPolicyOptions> policy, ILogger<PasswordPolicyProvider> logger, IPasswordPolicyMemoryCache cache, IDeviceProvider deviceProvider, IAadGraphApiProvider graphProvider, IAmsGroupProvider groupProvider)
        {
            this.policy = policy;
            this.logger = logger;
            this.cache = cache;
            this.deviceProvider = deviceProvider;
            this.graphProvider = graphProvider;
            this.groupProvider = groupProvider;
        }

        public async Task<PasswordPolicy> GetPolicy(string deviceId)
        {
            PasswordPolicyOptions options = this.policy.CurrentValue;

            if (options.Policies == null || options.Policies.Count == 0)
            {
                this.logger.LogTrace("No policies exist, therefore returning default policy for device {deviceId}", deviceId);
                return new PasswordPolicy(options.DefaultPolicy);
            }

            if (this.cache.TryGetValue(deviceId, out PasswordPolicy value))
            {
                this.logger.LogTrace("Found password policy for device {deviceId} in cache", deviceId);
                return value;
            }

            PasswordPolicyEntry selectedPolicy = await this.GetDevicePolicy(deviceId);
            if (selectedPolicy != null)
            {
                this.logger.LogTrace("Found password policy {passwordPolicyName}/{passwordPolicyId} for device {deviceId}", selectedPolicy.Name, selectedPolicy.Id, deviceId);
            }
            else
            {
                this.logger.LogTrace("No password policy matches found, therefore returning default policy for device {deviceId}", deviceId);
                selectedPolicy = options.DefaultPolicy;
            }

            PasswordPolicy devicePolicy = new PasswordPolicy(selectedPolicy);

            if (this.policy.CurrentValue.PolicyCacheDurationSeconds >= 0)
            {
                this.cache.Set(deviceId, devicePolicy, TimeSpan.FromSeconds(Math.Max(options.PolicyCacheDurationSeconds, 60)));
            }

            return devicePolicy;
        }

        private async Task<PasswordPolicyEntry> GetDevicePolicy(string deviceId)
        {
            IDevice device = await this.deviceProvider.GetDeviceAsync(deviceId);
            PasswordPolicyOptions options = this.policy.CurrentValue;

            if (options.Policies.Count == 0)
            {
                return null;
            }

            if (device.AuthorityType == AuthorityType.AzureActiveDirectory)
            {
                return await this.GetAadDevicePolicy(device, options);
            }

            if (device.AuthorityType == AuthorityType.Ams)
            {
                return await this.GetAmsDevicePolicy(device, options);
            }

            return null;
        }

        private async Task<PasswordPolicyEntry> GetAadDevicePolicy(IDevice device, PasswordPolicyOptions options)
        {
            List<SecurityIdentifier> items = await this.graphProvider.GetDeviceGroupSids(device.AuthorityId, device.AuthorityDeviceId);
            items.AddRange(this.groupProvider.GetGroupSidsForDevice(device).ToEnumerable());
            items.Add(new SecurityIdentifier(device.Sid));

            return this.GetMatchingPasswordPolicyEntryOrDefault(options, items);
        }

        private async Task<PasswordPolicyEntry> GetAmsDevicePolicy(IDevice device, PasswordPolicyOptions options)
        {
            if (options.Policies.All(t => t.TargetType != AuthorityType.Ams))
            {
                return null;
            }

            List<SecurityIdentifier> items = await this.groupProvider.GetGroupSidsForDevice(device).ToListAsync();
            items.Add(new SecurityIdentifier(device.Sid));

            return this.GetMatchingPasswordPolicyEntryOrDefault(options, items);
        }

        private PasswordPolicyEntry GetMatchingPasswordPolicyEntryOrDefault(PasswordPolicyOptions options, List<SecurityIdentifier> items)
        {
            foreach (PasswordPolicyEntry entry in options.Policies)
            {
                if (items.Any(t => string.Equals(entry.TargetGroup, t.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
