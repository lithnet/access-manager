using System;
using System.Security.AccessControl;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class MainWindowViewModel : Screen, IHandle<ModelChangedEvent>
    {
        private readonly IEventAggregator eventAggregator;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly ILogger<MainWindowViewModel> logger;

        public ApplicationConfigViewModel Config { get; set; }

        public MainWindowViewModel(ApplicationConfigViewModel c, IEventAggregator eventAggregator, IDialogCoordinator dialogCoordinator, ILogger<MainWindowViewModel> logger)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.eventAggregator = eventAggregator;
            this.eventAggregator.Subscribe(this);
            this.DisplayName = "Lithnet Access Manager Service (AMS) Configuration";
            this.Config = c;
        }

        public async Task<bool> Save()
        {
            if (await this.Config.Save())
            {
                this.IsDirty = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Close()
        {
            this.RequestClose();
        }

        public void Help()
        {

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
                    this.logger.LogError(ex, "Unable to save the configuration");
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

        public void Handle(ModelChangedEvent message)
        {
            //System.Diagnostics.Debug.WriteLine($"Model changed event received {message.Sender.GetType()}:{message.PropertyName}");
            this.IsDirty = true;
        }
    }
}
