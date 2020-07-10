using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for ChildWindow.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class ExternalDialogWindow : MetroWindow
    {
        public ExternalDialogWindow()
        {
            InitializeComponent();
            this.SaveButton.Focus();
        }

        public bool CancelButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = true;

        public bool CancelButtonIsDefault { get; set; } = true;

        public bool SaveButtonIsDefault { get => !this.CancelButtonIsDefault; set => this.CancelButtonIsDefault = !value; }

        public string SaveButtonName { get; set; } = "Save";

        public string CancelButtonName { get; set; } = "Cancel";

        public MessageDialogResult Result { get; set; }

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