using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public interface IWindowsServiceProvider
    {
        SecurityIdentifier ServiceSid { get; }

        ServiceControllerStatus Status { get; }

        SecurityIdentifier GetServiceSid();
        NTAccount GetServiceNTAccount();
        void SetServiceAccount(string username, string password);

        Task StartServiceAsync();

        Task StopServiceAsync();

        Task WaitForStatus(ServiceControllerStatus status);
    }
}