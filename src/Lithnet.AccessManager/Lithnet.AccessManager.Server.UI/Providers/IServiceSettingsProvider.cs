using System.Security.Principal;
using System.ServiceProcess;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IServiceSettingsProvider
    {
        SecurityIdentifier GetServiceAccount();
     
        void SetServiceAccount(string username, string password);

        ServiceController ServiceController { get; }
    }
}