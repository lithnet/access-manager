using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Server
{
    public class WindowsServiceProvider : IWindowsServiceProvider
    {
        private const uint SERVICE_NO_CHANGE = 0xffffffff;

        private readonly IClusterProvider clusterProvider;
        private readonly ILogger<WindowsServiceProvider> logger;
        private readonly ServiceController serviceController = new ServiceController(AccessManager.Constants.ServiceName);
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

        public WindowsServiceProvider(IClusterProvider clusterProvider, ILogger<WindowsServiceProvider> logger)
        {
            this.clusterProvider = clusterProvider;
            this.logger = logger;
        }

        public SecurityIdentifier ServiceSid { get; } = new SecurityIdentifier(serviceSidString);

        public SecurityIdentifier GetServiceAccount()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{AccessManager.Constants.ServiceName}", false);

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
                this.clusterProvider.StopClusteredResource();
            }
            else
            {
                this.serviceController.Stop();
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
                this.clusterProvider.StartClusteredResource();
            }
            else
            {
                this.serviceController.Start();
            }

            await this.serviceController.WaitForStatusAsync(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30), CancellationToken.None);
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
