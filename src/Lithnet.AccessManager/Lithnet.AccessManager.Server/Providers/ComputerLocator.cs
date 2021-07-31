using Lithnet.AccessManager.Api;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public class ComputerLocator : IComputerLocator
    {
        private readonly IActiveDirectory directory;
        private readonly IDiscoveryServices discoveryServices;
        private readonly ILogger<ComputerLocator> logger;
        private readonly IDeviceProvider dbDeviceProvider;
        private readonly IDataProtector protector;
        private readonly IMemoryCache memoryCache;
        private readonly IAadGraphApiProvider aadGraphProvider;

        private static string ProtectionPurpose = "computer-key-protector";

        public ComputerLocator(IActiveDirectory directory, ILogger<ComputerLocator> logger, IDeviceProvider dbDeviceProvider, IDataProtectionProvider dataProtectionProvider, IDiscoveryServices discoveryServices, IMemoryCache memoryCache, IAadGraphApiProvider aadGraphProvider)
        {
            this.directory = directory;
            this.logger = logger;
            this.dbDeviceProvider = dbDeviceProvider;
            this.discoveryServices = discoveryServices;
            this.memoryCache = memoryCache;
            this.aadGraphProvider = aadGraphProvider;
            this.protector = dataProtectionProvider.CreateProtector(ProtectionPurpose);
        }

        public async Task<ComputerSearchResult> FindComputerSingle(string searchText)
        {
            var computers = await this.FindComputers(searchText);

            if (computers.Count > 1)
            {
                throw new AmbiguousNameException($"There was more that one computer found for the name '{searchText}'");
            }

            return computers[0];
        }

        public async Task<List<ComputerSearchResult>> FindComputers(string searchText)
        {
            Dictionary<string, IComputer> foundComputers = new Dictionary<string, IComputer>();

            if (searchText.Contains("\\"))
            {
                string[] split = searchText.Split('\\');
                string domain = split[0];
                string host = split[1];

                if (string.Equals(domain, "AccessManager", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var device in (await this.dbDeviceProvider.FindDevices(host)).Where(t => t.AuthorityType == AuthorityType.Ams))
                    {
                        foundComputers.TryAdd(device.Sid, (IComputer)device);
                    }
                }
                else if (string.Equals(domain, "AzureAD", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var device in (await this.dbDeviceProvider.FindDevices(host)).Where(t => t.AuthorityType ==  AuthorityType.AzureActiveDirectory))
                    {
                        foundComputers.TryAdd(device.Sid, (IComputer)device);
                    }
                }
                else
                {
                    foreach (var computer in this.directory.GetComputers(searchText))
                    {
                        foundComputers.TryAdd(computer.Sid.ToString(), computer);
                    }
                }
            }
            else
            {
                foreach (var device in await this.dbDeviceProvider.FindDevices(searchText))
                {
                    foundComputers.TryAdd(device.Sid, (IComputer)device);
                }

                foreach (var computer in this.directory.GetComputers(searchText))
                {
                    foundComputers.TryAdd(computer.Sid.ToString(), computer);
                }
            }

            if (foundComputers.Count > 100)
            {
                throw new TooManyResultsException($"The search for computer name '{searchText}' returned {foundComputers.Count} results which exceeded the search result limit");
            }

            return await this.CreateSearchResults(foundComputers.Values.ToList<IComputer>());
        }

        public async Task<IComputer> GetComputer(string authorityId, AuthorityType authorityType, string authorityDeviceId)
        {
            if (authorityType == AuthorityType.ActiveDirectory)
            {
                if (authorityDeviceId.TryParseAsSid(out SecurityIdentifier sid))
                {
                    return this.directory.GetComputer(sid);
                }
                else
                {
                    throw new ObjectNotFoundException("Unknown authority device ID");
                }
            }
            else if (authorityType == AuthorityType.Ams || authorityType == AuthorityType.AzureActiveDirectory)
            {
                return (IComputer)await this.dbDeviceProvider.GetDeviceAsync(authorityType, authorityId, authorityDeviceId);
            }

            throw new ObjectNotFoundException("Unknown authority type");
        }

        public string GetKeyForComputer(IComputer computer)
        {
            DeviceIdentifier device = new DeviceIdentifier
            {
                AuthorityId = computer.AuthorityId,
                AuthorityType = computer.AuthorityType,
                AuthorityDeviceId = computer.AuthorityDeviceId,
                Sid = computer.SecurityIdentifier.ToString()
            };

            var data = JsonSerializer.Serialize(device);

            this.memoryCache.GetOrCreate<IComputer>(device.Sid, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return computer;
            });

            return this.protector.Protect(data);
        }

        public async Task<IComputer> GetComputerFromKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var data = this.protector.Unprotect(key);
            var device = JsonSerializer.Deserialize<DeviceIdentifier>(data);

            if (device == null ||
                string.IsNullOrWhiteSpace(device.AuthorityDeviceId) ||
                string.IsNullOrWhiteSpace(device.Sid) ||
                string.IsNullOrWhiteSpace(device.AuthorityId))
            {
                throw new InvalidOperationException("The deserialized object was not in a correct state");
            }

            if (this.memoryCache.TryGetValue(device.Sid, out IComputer computer))
            {
                this.logger.LogTrace("Found computer key in cache");
                return computer;
            }

            return await this.GetComputer(device.AuthorityId, device.AuthorityType, device.AuthorityDeviceId);
        }

        private async Task<List<ComputerSearchResult>> CreateSearchResults(IEnumerable<IComputer> results)
        {
            List<ComputerSearchResult> matches = new List<ComputerSearchResult>();

            foreach (var item in results.OrderByDescending(t => t.LastActivity))
            {
                string authorityName = item.AuthorityType.ToDescription();

                try
                {
                    if (item.AuthorityType == AuthorityType.ActiveDirectory)
                    {
                        authorityName = $"{this.discoveryServices.GetDomainNameDns(item.SecurityIdentifier)} ({item.AuthorityType.ToDescription()})";
                    }
                    else if (item.AuthorityType == AuthorityType.AzureActiveDirectory)
                    {
                        string tenantName = await this.aadGraphProvider.GetTenantOrgName(item.AuthorityId);
                        if (tenantName != null)
                        {
                            authorityName = $"{tenantName} ({item.AuthorityType.ToDescription()})";
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Unable to get authority name for device {item.AuthorityDeviceId}");
                }

                matches.Add(new ComputerSearchResult
                {
                    AuthorityName = authorityName,
                    DnsName = item.DnsHostName,
                    Key = this.GetKeyForComputer(item),
                    Name = item.FullyQualifiedName,
                    LastUpdate = item.LastActivity?.ToLocalTime().ToString(CultureInfo.CurrentCulture)
                });
            }

            return matches;
        }
    }
}
