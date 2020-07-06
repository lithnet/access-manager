using System;
using System.Threading.Tasks;
using Community.Windows.Forms;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ApplicationConfigViewModel : Conductor<PropertyChangedBase>.Collection.OneActive
    {
        private readonly IApplicationConfig model;

        private readonly IDialogCoordinator dialogCoordinator;

        public ApplicationConfigViewModel(
            IApplicationConfig model,
            IDialogCoordinator dialogCoordinator,
            AuthenticationViewModel authentication,
            AuthorizationViewModel authorization,
            UserInterfaceViewModel ui,
            RateLimitsViewModel rate,
            IpDetectionViewModel ip,
            AuditingViewModel audit,
            EmailViewModel mail,
            HostingViewModel hosting)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;

            this.Items.Add(hosting);
            this.Items.Add(authentication);
            this.Items.Add(audit);
            this.Items.Add(authorization);
            this.Items.Add(ui);
            this.Items.Add(mail);
            this.Items.Add(rate);
            this.Items.Add(ip);
        }

        public void Save()
        {
            try
            {
                this.model.Save(this.model.Path);
            }
            catch (Exception ex)
            {
                this.dialogCoordinator.ShowMessageAsync(this, "Error saving file", $"The configuration file could not be saved\r\n{ex.Message}");
            }
        }
    }
}
