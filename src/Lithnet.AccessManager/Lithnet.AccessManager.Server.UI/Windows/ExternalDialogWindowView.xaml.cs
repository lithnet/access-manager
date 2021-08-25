using System;
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
            this.Loaded += this.ExternalDialogWindowView_Loaded;
        }

        private void ExternalDialogWindowView_Loaded(object sender, RoutedEventArgs e)
        {
            this.CenterWindowOnApplication();

        }

        private void CenterWindowOnApplication()
        {
            System.Windows.Application curApp = System.Windows.Application.Current;
            Window mainWindow = curApp.MainWindow;
            if (mainWindow.WindowState == WindowState.Maximized)
            {
                // Get the mainWindow's screen:
                //var screen = Window..Screen.FromRectangle(new System.Drawing.Rectangle((int)mainWindow.Left, (int)mainWindow.Top, (int)mainWindow.Width, (int)mainWindow.Height));
                //double screenWidth = screen.WorkingArea.Width;
                //double screenHeight = screen.WorkingArea.Height;
                //double popupwindowWidth = this.Width;
                //double popupwindowHeight = this.Height;
                //this.Left = (screenWidth / 2) - (popupwindowWidth / 2);
                //this.Top = (screenHeight / 2) - (popupwindowHeight / 2);
            }
            else
            {
                this.Left = mainWindow.Left + ((mainWindow.ActualWidth - this.ActualWidth) / 2);
                this.Top = mainWindow.Top + ((mainWindow.ActualHeight - this.ActualHeight) / 2);
            }
        }
    }
}