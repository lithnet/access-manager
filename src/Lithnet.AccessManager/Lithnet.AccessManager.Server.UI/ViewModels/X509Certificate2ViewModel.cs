using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Threading.Tasks;
using Lithnet.Security.Authorization;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Server.UI
{
    public class X509Certificate2ViewModel : PropertyChangedBase
    {
        private readonly IServiceSettingsProvider serviceSettings;

        private readonly ILogger<X509Certificate2ViewModel> logger;

        private readonly IDialogCoordinator dialogCoordinator;

        public X509Certificate2ViewModel(X509Certificate2 model, IServiceSettingsProvider serviceSettings, ILogger<X509Certificate2ViewModel> logger, IDialogCoordinator dialogCoordinator)
        {
            this.Model = model;
            this.serviceSettings = serviceSettings;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.CheckCertificatePermissions();
        }

        public X509Certificate2 Model { get; }

        public string Subject => this.Model?.Subject;

        public DateTime NotBefore => this.Model.NotBefore;

        public DateTime NotAfter => this.Model.NotAfter;

        public bool IsPublished { get; set; }

        public bool IsOrphaned { get; set; }

        public bool HasPrivateKey => this.Model.HasPrivateKey;

        public bool HasNoPrivateKey => !this.HasPrivateKey;

        public bool HasPermission { get; set; }

        public bool HasNoPermission { get; set; }

        public bool HasPermissionError { get; set; }

        public string PermissionError { get; set; }

        public bool CanRepermission => !this.HasPermission;

        public async Task Repermission()
        {
            try
            {
                this.Model.AddPrivateKeyReadPermission(this.serviceSettings.GetServiceAccount());
            }
            catch(Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Could not set permissions on the private key");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not set permissions on the private key\r\n{ex.Message}");
            }

            this.CheckCertificatePermissions();
        }

        private void CheckCertificatePermissions()
        {
            this.HasPermission = false;
            this.HasNoPermission = false;
            this.HasPermissionError = false;
            this.PermissionError = null;

            if (this.HasNoPrivateKey)
            {
                return;
            }

            try
            {
                var security = this.Model.GetPrivateKeySecurity();
                using AuthorizationContext c = new AuthorizationContext(this.serviceSettings.GetServiceAccount());
                GenericSecurityDescriptor sd = new RawSecurityDescriptor(security.GetSecurityDescriptorSddlForm(AccessControlSections.All));
                this.HasPermission = c.AccessCheck(sd, (int)FileSystemRights.Read);
                this.HasNoPermission = !this.HasPermission;
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not read private key security for certificate {thumbprint}", this.Model.Thumbprint);
                this.HasPermissionError = true;
                this.PermissionError = $"Error: {ex.Message}";
            }
        }
    }
}
