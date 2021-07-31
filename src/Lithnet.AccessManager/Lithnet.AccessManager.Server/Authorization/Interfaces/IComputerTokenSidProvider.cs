using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IComputerTokenSidProvider
    {
        Task<List<SecurityIdentifier>> GetTokenSids(IComputer computer);
        
        Task<List<SecurityIdentifier>> GetTokenSidsForAadDevice(IDevice device);
        
        List<SecurityIdentifier> GetTokenSidsForAdDevice(IActiveDirectoryComputer computer);
        
        Task<List<SecurityIdentifier>> GetTokenSidsForAmsDevice(IDevice device);
    }
}