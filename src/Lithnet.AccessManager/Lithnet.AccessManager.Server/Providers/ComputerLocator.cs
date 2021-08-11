using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public class ComputerLocator : IComputerLocator
    {
        private readonly IActiveDirectory directory;
        private readonly ILogger<ComputerLocator> logger;
        private readonly IDeviceProvider dbDeviceProvider;
        public int MaximumSearchResults { get; set; } = 100;

        public ComputerLocator(IActiveDirectory directory, ILogger<ComputerLocator> logger, IDeviceProvider dbDeviceProvider)
        {
            this.directory = directory;
            this.logger = logger;
            this.dbDeviceProvider = dbDeviceProvider;
        }

        public async Task<List<IComputer>> FindComputers(string searchText)
        {
            Dictionary<string, IComputer> foundComputers = new Dictionary<string, IComputer>(StringComparer.OrdinalIgnoreCase);

            if (searchText.Contains("\\"))
            {
                string[] split = searchText.Split('\\');
                string domain = split[0];
                string host = split[1];

                if (string.Equals(domain, "AccessManager", StringComparison.OrdinalIgnoreCase))
                {
                    await this.FindAndAddDevices(host, AuthorityType.Ams, foundComputers);
                }
                else if (string.Equals(domain, "AzureAD", StringComparison.OrdinalIgnoreCase))
                {
                    await this.FindAndAddDevices(host, AuthorityType.AzureActiveDirectory, foundComputers);
                }
                else
                {
                    this.FindAndAddAdComputers(searchText, foundComputers);
                }
            }
            else
            {
                await this.FindAndAddDevices(searchText, AuthorityType.None, foundComputers);
                this.FindAndAddAdComputers(searchText, foundComputers);
            }

            if (foundComputers.Count > this.MaximumSearchResults)
            {
                throw new TooManyResultsException($"The search for computer name '{searchText}' returned {foundComputers.Count} results which exceeded the search result limit");
            }

            return foundComputers.Values.ToList<IComputer>();
        }

        private void FindAndAddAdComputers(string searchText, Dictionary<string, IComputer> foundComputers)
        {
            try
            {
                foreach (var computer in this.directory.GetComputers(searchText))
                {
                    foundComputers.TryAdd(computer.Sid.ToString(), computer);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred while searching the directory for matches to the name '{searchText}'. Search results may be incomplete");
            }
        }

        private async Task FindAndAddDevices(string searchText, AuthorityType authority, Dictionary<string, IComputer> foundComputers)
        {
            try
            {
                foreach (var device in (await this.dbDeviceProvider.FindDevices(searchText)).Where(t => authority == AuthorityType.None || t.AuthorityType == authority))
                {
                    foundComputers.TryAdd(device.Sid, (IComputer)device);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred while searching the database for matches to the name '{searchText}'. Search results may be incomplete");
            }
        }
    }
}
