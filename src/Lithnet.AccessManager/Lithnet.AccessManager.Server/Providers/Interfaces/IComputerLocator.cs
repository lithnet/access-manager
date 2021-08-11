using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IComputerLocator
    {
        int MaximumSearchResults { get; set; }
        Task<List<IComputer>> FindComputers(string searchText);
    }
}