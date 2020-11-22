using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class X509Certificate2ViewModel : PropertyChangedBase
    {
        private readonly ILogger<X509Certificate2ViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ICertificatePermissionProvider certPermissionProvider;

        public X509Certificate2ViewModel(X509Certificate2 model, ILogger<X509Certificate2ViewModel> logger, IDialogCoordinator dialogCoordinator, ICertificatePermissionProvider certPermissionProvider)
        {
            this.Model = model;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.certPermissionProvider = certPermissionProvider;
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

        public void Repermission()
        {
            try
            {
                this.certPermissionProvider.AddReadPermission(this.Model);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Could not set permissions on the private key");
                MessageBox.Show($"Could not set permissions on the private key\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                this.HasPermission = this.certPermissionProvider.ServiceAccountHasPermission(this.Model);
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
