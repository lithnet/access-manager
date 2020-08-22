using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ApplicationConfigViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        private readonly IApplicationConfig model;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly List<PropertyChangedBase> suspendedModels;

        public ApplicationConfigViewModel(IApplicationConfig model, IDialogCoordinator dialogCoordinator, AuthenticationViewModel authentication,
            AuthorizationViewModel authorization, UserInterfaceViewModel ui, RateLimitsViewModel rate, IpDetectionViewModel ip,
            AuditingViewModel audit, EmailViewModel mail, HostingViewModel hosting, ActiveDirectoryConfigurationViewModel ad,
            JitConfigurationViewModel jit, LapsConfigurationViewModel laps, HelpViewModel help, BitLockerViewModel bitLocker)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;

            this.hosting = hosting;
            this.Items.Add(hosting);

            this.suspendedModels = new List<PropertyChangedBase>();

            RegistryKey key = Registry.LocalMachine.OpenSubKey(AccessManager.Constants.BaseKey);
            bool currentlyUnconfigured = !(key?.GetValue("Configured", 0) is int value) || value == 0;
            if (currentlyUnconfigured)
            {
                this.suspendedModels.Add(authentication);
                this.suspendedModels.Add(ui);
                this.suspendedModels.Add(mail);
                this.suspendedModels.Add(rate);
                this.suspendedModels.Add(ip);
                this.suspendedModels.Add(ad);
                this.suspendedModels.Add(audit);
                this.suspendedModels.Add(laps);
                this.suspendedModels.Add(jit);
                this.suspendedModels.Add(bitLocker);
                this.suspendedModels.Add(authorization);
            }
            else
            {
                this.Items.Add(authentication);
                this.Items.Add(ui);
                this.Items.Add(mail);
                this.Items.Add(rate);
                this.Items.Add(ip);
                this.Items.Add(ad);
                this.Items.Add(audit);
                this.Items.Add(laps);
                this.Items.Add(jit);
                this.Items.Add(bitLocker);
                this.Items.Add(authorization);
            }

            this.OptionItems = new BindableCollection<PropertyChangedBase>();
            this.OptionItems.Add(help);

            this.ActiveItem = this.Items.First();
        }

        public BindableCollection<PropertyChangedBase> OptionItems { get; }

        private HostingViewModel hosting { get; }

        public string HelpLink => (this.ActiveItem as IHelpLink)?.HelpLink;

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

                if (this.suspendedModels.Count > 0)
                {
                    this.Items.AddRange(this.suspendedModels);
                    this.suspendedModels.Clear();
                }
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
