using System.Security.AccessControl;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class MainWindowViewModel : Screen, IHandle<ModelChangedEvent>
    {
        private readonly IEventAggregator eventAggregator;

        private readonly IDialogCoordinator dialogCoordinator;

        public ApplicationConfigViewModel Config { get; set; }

        public MainWindowViewModel(ApplicationConfigViewModel c, IEventAggregator eventAggregator, IDialogCoordinator dialogCoordinator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.eventAggregator = eventAggregator;
            this.eventAggregator.Subscribe(this);
            this.DisplayName = "Lithnet Access Manager Service (AMS) Configuration";
            this.Config = c;
        }

        public void Save()
        {
            this.Config.Save();
            this.IsDirty = false;
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
            if (this.IsDirty)
            {
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
                    this.Save();
                }
                else if (result == MessageDialogResult.Canceled || result == MessageDialogResult.Negative)
                {
                    return false;
                }
            }

            return true;
        }

        public string WindowTitle => $"{this.DisplayName}{(this.IsDirty ? "*" : "")}";

        public bool IsDirty { get; set; }

        public void Handle(ModelChangedEvent message)
        {
            System.Diagnostics.Debug.WriteLine($"Model changed event received {message.Sender.GetType()}:{message.PropertyName}");
            this.IsDirty = true;
        }
    }
}
