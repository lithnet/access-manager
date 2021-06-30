using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Api.Configuration;
using MahApps.Metro.IconPacks;
using Stylet;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryLithnetLapsConfigurationViewModel : Screen, IHelpLink
    {
        private readonly ApiAuthenticationOptions agentOptions;
        private readonly AmsManagedDeviceRegistrationOptions amsManagedDeviceOptions;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AmsDirectoryLithnetLapsConfigurationViewModel(IShellExecuteProvider shellExecuteProvider, ApiAuthenticationOptions agentOptions, AmsManagedDeviceRegistrationOptions amsManagedDeviceOptions, EncryptionCertificateComponentViewModel encryptionCertVm)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.agentOptions = agentOptions;
            this.amsManagedDeviceOptions = amsManagedDeviceOptions;
            this.PasswordEncryption = encryptionCertVm;
            this.DisplayName = "Lithnet LAPS";
        }

        public EncryptionCertificateComponentViewModel PasswordEncryption { get; set; }

        public string HelpLink => Constants.HelpLinkPageEmail;

        [NotifyModelChangedProperty]
        public bool AllowAmsManagedDeviceAuth { get => this.agentOptions.AllowAmsManagedDeviceAuth; set => this.agentOptions.AllowAmsManagedDeviceAuth = value; }

        [NotifyModelChangedProperty]
        public bool AutoApproveNewDevices { get => this.amsManagedDeviceOptions.AutoApproveNewDevices; set => this.amsManagedDeviceOptions.AutoApproveNewDevices = value; }

        // public PackIconUniconsKind Icon => PackIconUniconsKind.ServerConnection;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
