using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
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
            HostingViewModel hosting,
            ActiveDirectoryConfigurationViewModel ad,
            JitConfigurationViewModel jit,
            LapsConfigurationViewModel laps,
            HelpViewModel help)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;

            this.hosting = hosting;

            this.Items.Add(hosting);
            this.Items.Add(authentication);
            this.Items.Add(ad);
            this.Items.Add(audit);
            this.Items.Add(authorization);
            this.Items.Add(jit);
            this.Items.Add(laps);
            this.Items.Add(ui);
            this.Items.Add(mail);
            this.Items.Add(rate);
            this.Items.Add(ip);

            this.OptionItems = new BindableCollection<PropertyChangedBase>();
            this.OptionItems.Add(help);
         
            this.ActiveItem = this.Items.First();
        }

        public BindableCollection<PropertyChangedBase> OptionItems { get; }

        private HostingViewModel hosting { get; }

        public async Task<bool> Save()
        {
            try
            {
                this.model.Save(this.model.Path);
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error saving file", $"The configuration file could not be saved\r\n{ex.Message}");
                return false;
            }

            try
            {
                if (!await this.hosting.CommitSettings())
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error saving service configuration", $"There was a problem updating the service configuration\r\n{ex.Message}");
                return false;
            }

            try
            {
                RegistryKey key = Registry.LocalMachine.CreateSubKey(AccessManager.Constants.BaseKey, true);
                key.SetValue("Configured", 1);
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error writing to registry", $"There was a problem writing the updated config to the registry\r\n{ex.Message}");
                return false;
            }

            return true;
        }
    }
}
