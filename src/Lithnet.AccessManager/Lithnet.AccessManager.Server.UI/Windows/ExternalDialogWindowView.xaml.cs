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
    public partial class ExternalDialogWindowView : MetroWindow
    {
        public ExternalDialogWindowView()
        {
            InitializeComponent();
            this.SaveButton.Focus();
            this.Owner ??= Application.Current.Windows.OfType<Window>().FirstOrDefault(t => t.IsVisible && t.IsEnabled);
        }
    }
}