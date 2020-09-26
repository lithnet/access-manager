using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for ChildWindow.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class ImportWizardWindowView : MetroWindow
    {
        public ImportWizardWindowView()
        {
            InitializeComponent();
            if (Application.Current.MainWindow != this)
            {
                this.Owner ??= Application.Current.MainWindow;
            }
        }
    }
}