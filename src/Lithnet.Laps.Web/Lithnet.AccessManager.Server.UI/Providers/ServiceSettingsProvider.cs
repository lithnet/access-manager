using System.Security.Principal;
using System.ServiceProcess;
using Lithnet.AccessManager.Server.UI.Interop;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Server.UI
{
    public class ServiceSettingsProvider : IServiceSettingsProvider
    {
        public const string ServiceName = "lithnetadminaccesservice";

        public ServiceController ServiceController { get; } = new ServiceController(ServiceName);


        public SecurityIdentifier GetServiceAccount()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{ServiceName}", false);

            if (key == null)
            {
                return null;
            }

            var name = key.GetValue("ObjectName") as string;

            if (name == null)
            {
                return new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            }

            NTAccount account = new NTAccount(name);
            return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        }

        public void SetServiceAccount(string username, string password)
        {
            NativeMethods.ChangeServiceCredentials(ServiceName, username, password);
        }
    }
}
