using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IComputerLocator
    {
        Task<ComputerSearchResult> FindComputerSingle(string searchText);

        Task<List<ComputerSearchResult>> FindComputers(string searchText);

        Task<IComputer> GetComputer(string authorityId, AuthorityType authorityType, string authorityDeviceId);

        string GetKeyForComputer(IComputer computer);

        Task<IComputer> GetComputerFromKey(string key);
    }
}