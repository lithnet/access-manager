using Lithnet.AccessManager.Server.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class ComputerTokenSidProvider : IComputerTokenSidProvider
    {
        private readonly IAadGraphApiProvider aadProvider;
        private readonly IAmsGroupProvider amsGroupProvider;
        private readonly IActiveDirectory activeDirectory;

        public ComputerTokenSidProvider(IAadGraphApiProvider aadProvider, IAmsGroupProvider amsGroupProvider, IActiveDirectory activeDirectory)
        {
            this.aadProvider = aadProvider;
            this.amsGroupProvider = amsGroupProvider;
            this.activeDirectory = activeDirectory;
        }

        public async Task<List<SecurityIdentifier>> GetTokenSids(IComputer computer)
        {
            switch (computer.AuthorityType)
            {
                case AuthorityType.Ams:
                    if(!(computer is IDevice amsDevice))
                    {
                        throw new InvalidOperationException();
                    }

                    return await this.GetTokenSidsForAmsDevice(amsDevice);

                case AuthorityType.AzureActiveDirectory:
                    if (!(computer is IDevice aadDevice))
                    {
                        throw new InvalidOperationException();
                    }

                    return await this.GetTokenSidsForAadDevice(aadDevice);

                case AuthorityType.ActiveDirectory:
                    if (!(computer is IActiveDirectoryComputer adDevice))
                    {
                        throw new InvalidOperationException();
                    }

                    return this.GetTokenSidsForAdDevice(adDevice);

                default:
                    throw new NotSupportedException("The authority type was unknown or not supported");
            }
        }

        public async Task<List<SecurityIdentifier>> GetTokenSidsForAadDevice(IDevice device)
        {
            List<SecurityIdentifier> sids = await this.aadProvider.GetDeviceGroupSids(device.AuthorityId, device.AuthorityDeviceId);
            sids.AddRange(await this.amsGroupProvider.GetGroupSidsForDevice(device).ToListAsync());
            sids.Add(device.SecurityIdentifier);

            return sids;
        }

        public List<SecurityIdentifier> GetTokenSidsForAdDevice(IActiveDirectoryComputer computer)
        {
            return this.activeDirectory.GetTokenGroups(computer, computer.Sid.AccountDomainSid).ToList();
        }

        public async Task<List<SecurityIdentifier>> GetTokenSidsForAmsDevice(IDevice device)
        {
            List<SecurityIdentifier> sids = await this.amsGroupProvider.GetGroupSidsForDevice(device).ToListAsync();
            sids.Add(device.SecurityIdentifier);

            return sids;
        }
    }
}
