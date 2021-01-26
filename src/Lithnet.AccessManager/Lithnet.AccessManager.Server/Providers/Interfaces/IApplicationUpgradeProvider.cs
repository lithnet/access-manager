using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public interface IApplicationUpgradeProvider
    {
        Task<AppVersionInfo> GetVersionInfo();
    }
}