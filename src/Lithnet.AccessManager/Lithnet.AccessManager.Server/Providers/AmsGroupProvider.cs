using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public class AmsGroupProvider : IAmsGroupProvider
    {
        private readonly IDbAmsGroupProvider dbGroupProvider;
        private readonly IAmsSystemGroupProvider systemGroupProvider;
        private readonly ILogger<AmsGroupProvider> logger;

        public AmsGroupProvider(IDbAmsGroupProvider dbGroupProvider, ILogger<AmsGroupProvider> logger, IAmsSystemGroupProvider systemGroupProvider)
        {
            this.dbGroupProvider = dbGroupProvider;
            this.logger = logger;
            this.systemGroupProvider = systemGroupProvider;
        }

        public async IAsyncEnumerable<SecurityIdentifier> GetGroupSidsForDevice(IDevice device)
        {
            await foreach (var sid in this.dbGroupProvider.GetGroupSidsForDevice(device))
            {
                yield return sid;
            }

            foreach (var sid in this.systemGroupProvider.GetGroupSidsForDevice(device))
            {
                yield return sid;
            }
        }

        public async Task DeleteGroup(IAmsGroup group)
        {
            if (group.Type == AmsGroupType.System)
            {
                throw new NotSupportedException("Cannot delete a system group");
            }

            await this.dbGroupProvider.DeleteGroup(group);
        }

        public async Task RemoveFromGroup(IAmsGroup group, IDevice device)
        {
            if (group.Type == AmsGroupType.System)
            {
                throw new NotSupportedException("Cannot modify the membership of a system group");
            }

            await this.dbGroupProvider.RemoveFromGroup(group, device);
        }

        public async IAsyncEnumerable<IDevice> GetMemberDevices(IAmsGroup group)
        {
            HashSet<string> devicesFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var device in this.dbGroupProvider.GetMemberDevices(group))
            {
                if (devicesFound.Add(device.Sid))
                {
                    yield return device;
                }
            }

            await foreach (var device in this.systemGroupProvider.GetMemberDevices(group))
            {
                if (devicesFound.Add(device.Sid))
                {
                    yield return device;
                }
            }
        }

        public async Task AddToGroup(IAmsGroup group, IDevice device)
        {
            if (group.Type == AmsGroupType.System)
            {
                throw new NotSupportedException("Cannot modify the membership of a system group");
            }

            await this.dbGroupProvider.AddToGroup(group, device);
        }

        public async Task<IAmsGroup> CloneGroup(IAmsGroup group)
        {
            if (group.Type == AmsGroupType.System)
            {
                throw new NotSupportedException("Cannot clone a system group");
            }

            return await this.dbGroupProvider.CloneGroup(group);
        }

        public async Task<IAmsGroup> CreateGroup()
        {
            return await this.dbGroupProvider.CreateGroup();
        }

        public async Task<IAmsGroup> UpdateGroup(IAmsGroup group)
        {
            if (group.Type == AmsGroupType.System)
            {
                throw new NotSupportedException("Cannot modify a system group");
            }

            return await this.dbGroupProvider.UpdateGroup(group);
        }

        public async Task<IAmsGroup> GetGroupBySid(string groupSid)
        {
            if (SidUtils.IsAmsBuiltInSid(groupSid))
            {
                return this.systemGroupProvider.GetGroupBySid(groupSid);
            }
            else
            {
                return await this.dbGroupProvider.GetGroupBySid(groupSid);
            }
        }

        public async IAsyncEnumerable<IAmsGroup> GetGroups()
        {
            foreach (var group in this.systemGroupProvider.GetGroups())
            {
                yield return group;
            }

            await foreach (var group in this.dbGroupProvider.GetGroups())
            {
                yield return group;
            }
        }
    }
}