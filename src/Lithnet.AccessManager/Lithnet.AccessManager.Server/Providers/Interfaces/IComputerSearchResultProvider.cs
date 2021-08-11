using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IComputerSearchResultProvider
    {
        Task<ComputerSearchResult> FindComputerSingle(string searchText);

        Task<List<ComputerSearchResult>> FindComputers(string searchText);

        Task<IComputer> GetComputerFromKey(string key);
    }
}