using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IAuthorityDataProvider
    {
        Task<string> GetAuthorityName(IComputer item);
        Task<string> GetAuthorityNameAndType(IComputer item);
    }
}