using System;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Vanara.PInvoke;
using Vanara.Security.AccessControl;

namespace Lithnet.AccessManager.Server
{
    public class WindowsServiceProvider : IWindowsServiceProvider
    {
        private const uint SERVICE_NO_CHANGE = 0xffffffff;

        private readonly IClusterProvider clusterProvider;
        private readonly ILogger<WindowsServiceProvider> logger;
        private readonly ServiceController serviceController = new ServiceController(Constants.ServiceName);
        private readonly IDiscoveryServices discoveryServices;

        private const string serviceSidString = "S-1-5-80-125788923-1836679867-2653951330-153436886-93372159";

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ChangeServiceConfig(IntPtr hService, uint nServiceType, uint nStartType, uint nErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string pDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

        public ServiceControllerStatus Status
        {
            get
            {
                this.serviceController.Refresh();
                return this.serviceController.Status;
            }
        }

        public WindowsServiceProvider(IClusterProvider clusterProvider, ILogger<WindowsServiceProvider> logger, IDiscoveryServices discoveryServices)
        {
            this.clusterProvider = clusterProvider;
            this.logger = logger;
            this.discoveryServices = discoveryServices;
        }

        public SecurityIdentifier ServiceSid { get; } = new SecurityIdentifier(serviceSidString);

        public SecurityIdentifier GetServiceAccountSid()
        {
            string name = this.GetRegistryObjectName();

            if (name == null)
            {
                return new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            }

            NTAccount account = new NTAccount(name);
            return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        }

        public NTAccount GetServiceNTAccount()
        {
            string name = this.GetRegistryObjectName();

            if (name == null)
            {
                return (NTAccount)(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)));
            }

            return new NTAccount(name);
        }

        private string GetRegistryObjectName()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{AccessManager.Constants.ServiceName}", false);

            if (key == null)
            {
                return null;
            }

            return key.GetValue("ObjectName") as string;
        }

        public void SetServiceAccount(string username, string password)
        {
            ChangeServiceCredentials(Constants.ServiceName, username, password);
        }

        public async Task WaitForStatus(ServiceControllerStatus status)
        {
            await this.serviceController.WaitForStatusAsync(status, TimeSpan.FromSeconds(30), CancellationToken.None);
        }

        public async Task StopServiceAsync()
        {
            this.serviceController.Refresh();

            if (this.serviceController.Status == ServiceControllerStatus.Stopped)
            {
                return;
            }

            if (this.clusterProvider.IsClustered)
            {
                await Task.Run(() => this.clusterProvider.StopClusteredResource());
            }
            else
            {
                await Task.Run(() => this.serviceController.Stop());
            }

            await this.serviceController.WaitForStatusAsync(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30), CancellationToken.None);
        }


        public async Task StartServiceAsync()
        {
            this.serviceController.Refresh();

            if (this.serviceController.Status == ServiceControllerStatus.Running)
            {
                return;
            }

            if (this.clusterProvider.IsClustered)
            {
                await Task.Run(() => this.clusterProvider.StartClusteredResource());
            }
            else
            {
                await Task.Run(() => this.serviceController.Start());
            }

            await this.serviceController.WaitForStatusAsync(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30), CancellationToken.None);
        }

        public void GrantLogonAsAService(string accountName)
        {
            SystemSecurity d = new SystemSecurity();
            var privs = d.UserPrivileges(accountName);

            if (!privs[SystemPrivilege.ServiceLogon])
            {
                logger.LogInformation("Granting logon as a service right to account {account}", accountName);
                privs[SystemPrivilege.ServiceLogon] = true;
            }
        }

        public bool CanGmsaBeUsedOnThisMachine(string samAccountName)
        {
            var result = NetApi32.NetQueryServiceAccount(null, samAccountName, 0, out NetApi32.SafeNetApiBuffer buffer);
            result.ThrowIfFailed();

            MsaInfo0 msaInfo = buffer.ToStructure<MsaInfo0>();

            this.logger.LogTrace($"NetQueryServiceAccount returned {msaInfo.State}");

            return msaInfo.State == MsaInfoState.MsaInfoInstalled;
        }

        public int LogonServiceAccount(ISecurityPrincipal o, string password)
        {
            var domain = this.discoveryServices.GetDomainNameNetBios(o.Sid);

            if (AdvApi32.LogonUser(o.SamAccountName, domain, password, AdvApi32.LogonUserType.LOGON32_LOGON_SERVICE, AdvApi32.LogonUserProvider.LOGON32_PROVIDER_DEFAULT, out AdvApi32.SafeHTOKEN token))
            {
                return 0;
            }

            int result = Marshal.GetLastWin32Error();
            Exception ex = new Win32Exception(result);
            this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to validate credentials");

            return result;
        }

    private static void ChangeServiceCredentials(string serviceName, string username, string password)
    {
        ServiceController controller = new ServiceController(serviceName);

        if (!ChangeServiceConfig(controller.ServiceHandle.DangerousGetHandle(), SERVICE_NO_CHANGE, SERVICE_NO_CHANGE, SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, username, password, null))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
}
