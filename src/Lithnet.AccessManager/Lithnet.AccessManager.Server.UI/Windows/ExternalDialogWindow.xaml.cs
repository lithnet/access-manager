using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for ChildWindow.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class ExternalDialogWindow : MetroWindow
    {
        private readonly IShellExecuteProvider shellExecuteProvider;

        public ExternalDialogWindow()
        {
            InitializeComponent();
            this.SaveButton.Focus();
            this.Owner ??= Application.Current.Windows.OfType<Window>().FirstOrDefault(t => t.IsVisible && t.IsEnabled);
            this.Activated += this.ExternalDialogWindow_Activated;
        }

        private void ExternalDialogWindow_Activated(object sender, System.EventArgs e)
        {
            if (this.DataContext is IScreenState s)
            {
                s.Activate();
            }
        }

        public ExternalDialogWindow(IShellExecuteProvider shellExecuteProvider)
        : this()
        {
            this.shellExecuteProvider = shellExecuteProvider;
        }

        public bool CancelButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = true;

        public bool CancelButtonIsDefault { get; set; } = false;

        public bool SaveButtonIsDefault { get; set; } = true;

        public string SaveButtonName { get; set; } = "Save";

        public string CancelButtonName { get; set; } = "Cancel";

        public MessageDialogResult Result { get; set; }

        public string HelpLink => (this.DataContext as IHelpLink)?.HelpLink;

        public async Task Help()
        {
            if (this.HelpLink == null || this.shellExecuteProvider == null)
            {
                return;
            }

            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this.DataContext is INotifyDataErrorInfo vm && vm.HasErrors)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }
    }
}