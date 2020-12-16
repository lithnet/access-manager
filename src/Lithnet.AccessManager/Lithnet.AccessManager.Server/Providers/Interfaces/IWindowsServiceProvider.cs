using System.Security.Principal;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public interface IWindowsServiceProvider
    {
        SecurityIdentifier ServiceSid { get; }

        ServiceControllerStatus Status { get; }

        bool CanGmsaBeUsedOnThisMachine(string samAccountName);
        SecurityIdentifier GetServiceAccountSid();
        NTAccount GetServiceNTAccount();
        void GrantLogonAsAService(string accountName);
        int LogonServiceAccount(ISecurityPrincipal o, string password);
        void SetServiceAccount(string username, string password);

        Task StartServiceAsync();

        Task StopServiceAsync();

        Task WaitForStatus(ServiceControllerStatus status);
    }
}