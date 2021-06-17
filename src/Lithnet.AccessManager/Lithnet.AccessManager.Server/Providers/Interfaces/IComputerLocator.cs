using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IComputerLocator
    {
        Task<IComputer> FindComputerSingle(string searchText);
        Task<IList<IComputer>> FindComputers(string searchText);
    }
}