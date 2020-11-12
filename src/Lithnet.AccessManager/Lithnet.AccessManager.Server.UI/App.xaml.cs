using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(TextBox), Control.MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText));

            EventManager.RegisterClassHandler(typeof(PasswordBox), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(PasswordBox), UIElement.GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(PasswordBox), Control.MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText));

            Window WpfBugWindow = new Window()
            {
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                WindowStyle = WindowStyle.None,
                Top = 0,
                Left = 0,
                Width = 1,
                Height = 1,
                ShowInTaskbar = false
            };

            WpfBugWindow.Show();

            try
            {
                base.OnStartup(e);
                ShutdownMode = ShutdownMode.OnLastWindowClose;
                WpfBugWindow.Close();
            }
            catch (MissingConfigurationException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                WpfBugWindow.Close();
                this.Shutdown(1);
            }
            catch (ClusterNodeNotActiveException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                WpfBugWindow.Close();
                this.Shutdown(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was a problem loading the application, and the application will now terminate\r\n\r\nError message: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                WpfBugWindow.Close();
                this.Shutdown(1);
            }
        }

        void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            DependencyObject parent = e.OriginalSource as UIElement;

            while (parent != null && (!(parent is TextBox) && !(parent is PasswordBox)))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is Control textBox)
            {
                if (!textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        void SelectAllText(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
            {
                textBox.SelectAll();
                return;
            }

            if (e.OriginalSource is PasswordBox passwordBox)
            {
                passwordBox.SelectAll();
            }
        }
    }
}
