using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Providers
{
    public class ComputerLocator : IComputerLocator
    {
        private readonly IActiveDirectory directory;
        private readonly ILogger<ComputerLocator> logger;
        private readonly IDeviceProvider dbDeviceProvider;

        public ComputerLocator(IActiveDirectory directory, ILogger<ComputerLocator> logger, IDeviceProvider dbDeviceProvider)
        {
            this.directory = directory;
            this.logger = logger;
            this.dbDeviceProvider = dbDeviceProvider;
        }

        public async Task<IComputer> FindComputerSingle(string searchText)
        {
            var computers = await this.FindComputers(searchText);

            if (computers.Count > 1)
            {
                throw new AmbiguousNameException($"There was more that one computer found for the name '{searchText}'");
            }

            return computers[0];
        }

        public async Task<IList<IComputer>> FindComputers(string searchText)
        {
            List<IComputer> foundComputers = new List<IComputer>();

            foreach (var device in await this.dbDeviceProvider.FindDevices(searchText))
            {
                foundComputers.Add((IComputer)device);
            }

            if (this.TryFindComputerInAd(searchText, out IComputer computer))
            {
                if (foundComputers.All(t => t.SecurityIdentifier != computer.SecurityIdentifier))
                {
                    foundComputers.Add(computer);
                }
            }

            return foundComputers;
        }

        private IComputer GetFromActiveDirectory(IDevice device)
        {
            if (device.AuthorityType == AuthorityType.ActiveDirectory)
            {
                return this.directory.GetComputer(device.Sid);
            }

            return device as IComputer;
        }

        private bool TryFindComputerInAd(string searchText, out IComputer computer)
        {
            computer = null;

            try
            {
                computer = this.directory.GetComputer(searchText);
                return true;
            }
            catch (ObjectNotFoundException)
            {
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error searching the directory for '{searchText}'");
            }

            return false;
        }
    }
}
