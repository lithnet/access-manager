using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public class AmsSystemGroupProvider : IAmsSystemGroupProvider
    {
        private readonly IDeviceProvider deviceProvider;
        private readonly ILogger<AmsSystemGroupProvider> logger;
        private static Dictionary<string, BuiltInAmsGroup> builtInGroups;
        private const string SidAllDevices = "S-1-4096-2-1";
        private const string SidAllAmsDevices = "S-1-4096-2-2";
        private const string SidAllAzureDevices = "S-1-4096-2-3";
        private const string SidAllWindowsDevices = "S-1-4096-2-4";
        private const string SidAllLinuxDevices = "S-1-4096-2-5";
        private const string SidAllMacOsDevices = "S-1-4096-2-6";
        private const string SidAllAmsWindowsDevices = "S-1-4096-2-7";
        private const string SidAllAzureWindowsDevices = "S-1-4096-2-8";
        private const string SidAllAmsLinuxDevices = "S-1-4096-2-9";
        private const string SidAllAmsMacOsDevices = "S-1-4096-2-10";

        static AmsSystemGroupProvider()
        {
            builtInGroups = new Dictionary<string, BuiltInAmsGroup>()
            {
                {SidAllDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllDevices), "All devices", "All devices registered on this AMS server",
                    device => true)
                },

                {SidAllAmsDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllAmsDevices), "AMS devices", "Devices registered using AMS authentication",
                    device => device.AuthorityType == AuthorityType.Ams)},

                {SidAllAzureDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllAzureDevices), "Azure AD devices", "Devices registered using Azure authentication",
                    device => device.AuthorityType == AuthorityType.AzureActiveDirectory)},

                {SidAllWindowsDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllWindowsDevices), "All Windows devices", "All Windows devices",
                    device => device.OperatingSystemType == Api.Shared.OsType.Windows)},

                {SidAllLinuxDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllLinuxDevices), "All Linux devices", "All Linux devices",
                    device => device.OperatingSystemType == Api.Shared.OsType.Linux)},

                {SidAllMacOsDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllMacOsDevices), "All macOS devices", "All macOS devices",
                    device => device.OperatingSystemType == Api.Shared.OsType.MacOS)},

                {SidAllAmsWindowsDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllWindowsDevices), "AMS Windows devices", "Windows devices registered using AMS authentication",
                    device=> device.OperatingSystemType == Api.Shared.OsType.Windows && device.AuthorityType == AuthorityType.Ams)},

                {SidAllAmsLinuxDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllLinuxDevices), "AMS Linux devices", "Linux devices registered using AMS authentication",
                    device => device.OperatingSystemType == Api.Shared.OsType.Linux && device.AuthorityType == AuthorityType.Ams)},

                {SidAllAmsMacOsDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllMacOsDevices), "AMS macOS devices", "macOS devices registered using AMS authentication",
                    device => device.OperatingSystemType == Api.Shared.OsType.MacOS && device.AuthorityType == AuthorityType.Ams)},

                {SidAllAzureWindowsDevices, new BuiltInAmsGroup(new SecurityIdentifier(SidAllWindowsDevices), "Azure AD Windows devices", "Windows devices registered using Azure authentication",
                    device=> device.OperatingSystemType == Api.Shared.OsType.Windows && device.AuthorityType == AuthorityType.AzureActiveDirectory)},
            };
        }

        public AmsSystemGroupProvider(IDeviceProvider deviceProvider, ILogger<AmsSystemGroupProvider> logger)
        {
            this.deviceProvider = deviceProvider;
            this.logger = logger;
        }

        public IEnumerable<SecurityIdentifier> GetGroupSidsForDevice(IDevice device)
        {
            yield return builtInGroups[SidAllDevices].SecurityIdentifier;

            if (device.AuthorityType == AuthorityType.Ams)
            {
                yield return builtInGroups[SidAllAmsDevices].SecurityIdentifier;
            }
            else if (device.AuthorityType == AuthorityType.AzureActiveDirectory)
            {
                yield return builtInGroups[SidAllAzureDevices].SecurityIdentifier;
            }

            if (device.OperatingSystemType == Api.Shared.OsType.Windows)
            {
                yield return builtInGroups[SidAllWindowsDevices].SecurityIdentifier;

                if (device.AuthorityType == AuthorityType.Ams)
                {
                    yield return builtInGroups[SidAllAmsWindowsDevices].SecurityIdentifier;
                }
                else if (device.AuthorityType == AuthorityType.AzureActiveDirectory)
                {
                    yield return builtInGroups[SidAllAzureWindowsDevices].SecurityIdentifier;
                }
            }

            if (device.OperatingSystemType == Api.Shared.OsType.Linux)
            {
                yield return builtInGroups[SidAllLinuxDevices].SecurityIdentifier;

                if (device.AuthorityType == AuthorityType.Ams)
                {
                    yield return builtInGroups[SidAllAmsLinuxDevices].SecurityIdentifier;
                }
            }

            if (device.OperatingSystemType == Api.Shared.OsType.MacOS)
            {
                yield return builtInGroups[SidAllMacOsDevices].SecurityIdentifier;

                if (device.AuthorityType == AuthorityType.Ams)
                {
                    yield return builtInGroups[SidAllAmsMacOsDevices].SecurityIdentifier;
                }
            }
        }

        public async IAsyncEnumerable<IDevice> GetMemberDevices(IAmsGroup group)
        {
            if (group is BuiltInAmsGroup b)
            {
                await foreach (var device in this.deviceProvider.GetDevices())
                {
                    if (b.IsIncluded(device))
                    {
                        yield return device;
                    }
                }
            }
        }

        public IAmsGroup GetGroupBySid(string groupSid)
        {
            if (builtInGroups.TryGetValue(groupSid, out BuiltInAmsGroup group))
            {
                return group;
            }

            throw new GroupNotFoundException($"Could not find a group with SID {groupSid}");
        }

        public IEnumerable<IAmsGroup> GetGroups()
        {
            foreach (var group in builtInGroups.Values)
            {
                yield return group;
            }
        }
    }
}