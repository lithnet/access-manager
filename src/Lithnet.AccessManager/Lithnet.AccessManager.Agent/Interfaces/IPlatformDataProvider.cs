using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent
{
    public interface IPlatformDataProvider
    {
        string GetOSName();

        string GetOSVersion();

        string GetMachineName();

        string GetDnsName();

        OsType GetOsType();
    }
}