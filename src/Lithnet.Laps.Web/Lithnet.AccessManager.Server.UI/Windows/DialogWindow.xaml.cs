using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for ChildWindow.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class DialogWindow : ChildWindow
    {
        public DialogWindow()
        {
            InitializeComponent();
        }

        public bool CancelButtonIsDefault { get; set; } = true;

        public bool SaveButtonIsDefault { get => !this.CancelButtonIsDefault; set => this.CancelButtonIsDefault = !value; }

        public string SaveButtonName { get; set; } = "Save";

        public string CancelButtonName { get; set; } = "Cancel";

        public MessageDialogResult Result { get; set; }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MessageDialogResult.Canceled;
            this.Close();
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Result = MessageDialogResult.Affirmative;
            this.Close();
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var vm = this.DataContext as INotifyDataErrorInfo;

            if (vm != null && vm.HasErrors)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }
    }
}