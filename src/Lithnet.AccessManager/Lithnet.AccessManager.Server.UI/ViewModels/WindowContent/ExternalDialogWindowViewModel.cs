using Stylet;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace Lithnet.AccessManager.Server.UI
{
    public class ExternalDialogWindowViewModel : Conductor<Screen>
    {
        private readonly IShellExecuteProvider shellExecuteProvider;

        public ExternalDialogWindowViewModel(Screen viewModel, IShellExecuteProvider shellExecuteProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.ActiveItem = viewModel;
            this.ActiveItem.ErrorsChanged += this.ActiveItem_ErrorsChanged;
            this.DisplayName = this.ActiveItem.DisplayName ?? "Lithnet Access Manager";

            if (this.ActiveItem is IExternalDialogAware e)
            {
                this.CancelButtonIsDefault = e.CancelButtonIsDefault;
                this.CancelButtonName = e.CancelButtonName;
                this.CancelButtonVisible = e.CancelButtonVisible;
                this.SaveButtonIsDefault = e.SaveButtonIsDefault;
                this.SaveButtonName = e.SaveButtonName;
                this.SaveButtonVisible = e.SaveButtonVisible;
            }
        }

        private void ActiveItem_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            this.HasChildErrors = this.ActiveItem.HasErrors;
            this.NotifyOfPropertyChange(() => this.CanSave);
        }

        public bool CancelButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = true;

        public bool CancelButtonIsDefault { get; set; } = false;

        public bool SaveButtonIsDefault { get; set; } = true;

        public string SaveButtonName { get; set; } = "Save";

        public string CancelButtonName { get; set; } = "Cancel";

        public bool HasChildErrors { get; set; }

        public string HelpLink => (this.ActiveItem as IHelpLink)?.HelpLink;

        public async Task Help()
        {
            if (this.HelpLink == null || this.shellExecuteProvider == null)
            {
                return;
            }

            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public void Close()
        {
            this.RequestClose(false);
        }

        public bool CanSave
        {
            get
            {
                if (this.HasChildErrors)
                {
                    return false;
                }

                return true;
            }
        }

        public void Save()
        {
            this.RequestClose(true);
        }
    }
}
