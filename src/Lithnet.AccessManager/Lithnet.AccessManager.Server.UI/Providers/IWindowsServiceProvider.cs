using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IWindowsServiceProvider
    {
        ServiceControllerStatus Status { get; }

        SecurityIdentifier GetServiceAccount();
     
        void SetServiceAccount(string username, string password);

        Task StartServiceAsync();

        Task StopServiceAsync();

        Task WaitForStatus(ServiceControllerStatus status);
    }
}