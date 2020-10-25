using System;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.UI.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Server.UI
{
    public class WindowsServiceProvider : IWindowsServiceProvider
    {
        private readonly IClusterProvider clusterProvider;
        private readonly ILogger<WindowsServiceProvider> logger;
        private readonly ServiceController serviceController = new ServiceController(AccessManager.Constants.ServiceName);

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
            NativeMethods.ChangeServiceCredentials(AccessManager.Constants.ServiceName, username, password);
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
    }
}
