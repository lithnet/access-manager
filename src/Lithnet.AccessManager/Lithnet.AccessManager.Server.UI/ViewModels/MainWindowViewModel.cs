using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class MainWindowViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHandle<ModelChangedEvent>
    {
        private readonly IApplicationConfig model;

        private readonly IEventAggregator eventAggregator;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly ILogger<MainWindowViewModel> logger;

        private readonly IShellExecuteProvider shellExecuteProvider;

        private readonly IServiceSettingsProvider serviceSettingsProvider;

        public MainWindowViewModel(IApplicationConfig model, AuthenticationViewModel authentication, AuthorizationViewModel authorization, UserInterfaceViewModel ui, RateLimitsViewModel rate, IpDetectionViewModel ip,
            AuditingViewModel audit, EmailViewModel mail, HostingViewModel hosting, ActiveDirectoryConfigurationViewModel ad,
            JitConfigurationViewModel jit, LapsConfigurationViewModel laps, HelpViewModel help, BitLockerViewModel bitLocker, IEventAggregator eventAggregator, IDialogCoordinator dialogCoordinator, ILogger<MainWindowViewModel> logger, IShellExecuteProvider shellExecuteProvider, IServiceSettingsProvider serviceSettingsProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.serviceSettingsProvider = serviceSettingsProvider;
            this.eventAggregator = eventAggregator;
            this.eventAggregator.Subscribe(this);
            this.DisplayName = Constants.AppName;

            this.model = model;
            this.dialogCoordinator = dialogCoordinator;

            this.hosting = hosting;
            this.Items.Add(hosting);
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

            this.OptionItems = new BindableCollection<PropertyChangedBase>();
            this.OptionItems.Add(help);

            this.ActiveItem = this.Items.First();

            this.UpdateIsConfigured();
        }

        public BindableCollection<PropertyChangedBase> OptionItems { get; }

        private HostingViewModel hosting { get; }

        public PropertyChangedBase ActiveOptionsItem { get; set; }

        public string HelpLink => (this.ActiveItem as IHelpLink)?.HelpLink ?? (this.ActiveOptionsItem as IHelpLink)?.HelpLink;

        public async Task<bool> Save()
        {
            try
            {
                this.model.Save(this.model.Path);
                this.IsDirty = false;
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

            this.UpdateIsConfigured();

            if (this.IsRestartRequiredOnSave)
            {
                if (await this.dialogCoordinator.ShowMessageAsync(this, "Service configuration updated", $"The service must be restarted for the new settings to take effect. Do you want to restart the service now?", MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Yes",
                        NegativeButtonText = "No"
                    }) == MessageDialogResult.Affirmative)
                {
                    await this.RestartService();
                    this.IsRestartRequiredOnSave = false;
                }
                else
                {
                    this.IsPendingServiceRestart = true;
                    _ = Task.Run(() =>
                      {
                          try
                          {
                              this.serviceSettingsProvider.ServiceController.WaitForStatus(ServiceControllerStatus.Stopped);
                              this.serviceSettingsProvider.ServiceController.WaitForStatus(ServiceControllerStatus.Running);
                              this.IsPendingServiceRestart = false;
                          }
                          catch (Exception ex)
                          {
                              this.logger.LogError(EventIDs.UIGenericError, ex, "Service status polling error");
                          }
                      });
                }
            }

            return true;
        }


        public void Close()
        {
            this.RequestClose();
        }

        public async Task Help()
        {
            if (this.HelpLink == null)
            {
                return;
            }

            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public void About()
        {

        }

        public override async Task<bool> CanCloseAsync()
        {
            if (!this.IsDirty)
            {
                return true;
            }

            var result = await this.dialogCoordinator.ShowMessageAsync(this, "Unsaved changed",
                    "Do you want to save your changes?",
                    MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "_Save",
                        NegativeButtonText = "_Cancel",
                        FirstAuxiliaryButtonText = "Do_n't Save",
                        DefaultButtonFocus = MessageDialogResult.Affirmative,
                        AnimateShow = false,
                        AnimateHide = false
                    });

            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    return await this.Save();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Unable to save the configuration");
                    await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Unable to save the configuration\r\n{ex.Message}");
                }
            }
            else if (result == MessageDialogResult.FirstAuxiliary)
            {
                return true;
            }

            return false;
        }

        public string WindowTitle => $"{this.DisplayName}{(this.IsDirty ? "*" : "")}";

        public bool IsDirty { get; set; }

        public bool IsPendingServiceRestart { get; set; }

        public bool IsRestartRequiredOnSave { get; set; }

        public bool IsUnconfigured => !this.IsConfigured;

        public bool IsConfigured { get; set; }

        private void UpdateIsConfigured()
        {
            int? value = Registry.GetValue(AccessManager.Constants.RootedBaseKey, "Configured", 0) as int?;

            if (value == null)
            {
                this.IsConfigured = false;
            }
            else
            {
                this.IsConfigured = value == 1;
            }
        }

        public void Handle(ModelChangedEvent message)
        {
            this.IsDirty = true;

            if (message.RequiresServiceRestart)
            {
                this.IsRestartRequiredOnSave = true;
            }
        }

        public async Task RestartService()
        {
            try
            {
                var controller = this.serviceSettingsProvider.ServiceController;
                controller.Refresh();

                if (controller.Status == ServiceControllerStatus.Running)
                {
                    controller.Stop();
                    await controller.WaitForStatusAsync(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30), CancellationToken.None);
                }

                controller.Start();
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not restart service");
            }
        }
    }
}
