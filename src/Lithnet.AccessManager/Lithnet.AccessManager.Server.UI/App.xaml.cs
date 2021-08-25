using Microsoft.Extensions.Logging;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;

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

            EventManager.RegisterClassHandler(typeof(RadNavigationViewItem), RadNavigationViewItem.ClickEvent, new RoutedEventHandler(OnRadNavigationViewItemClick));

            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)));

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
                try
                {
                    var name = Forest.GetCurrentForest().RootDomain.GetDirectoryEntry().Name;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to get information about the current domain. Ensure the domain is contactable and try again.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Shutdown(1);
                    return;
                }

                base.OnStartup(e);
                ShutdownMode = ShutdownMode.OnLastWindowClose;
                WpfBugWindow.Close();
            }
            catch (MissingConfigurationException ex)
            {
                Bootstrapper.Logger?.LogCritical(EventIDs.UIInitializationError, ex, "Initialization error");
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                WpfBugWindow.Close();
                this.Shutdown(1);
            }
            catch (ClusterNodeNotActiveException ex)
            {
                Bootstrapper.Logger?.LogCritical(EventIDs.UIInitializationError, ex, "Initialization error");
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                WpfBugWindow.Close();
                this.Shutdown(1);
            }
            catch (Exception ex)
            {
                Bootstrapper.Logger?.LogCritical(EventIDs.UIInitializationError, ex, "Initialization error");
                MessageBox.Show($"There was a problem loading the application, and the application will now terminate\r\n\r\nError message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void OnRadNavigationViewItemClick(object sender, RoutedEventArgs e)
        {
            RadNavigationViewItem item = e.OriginalSource as RadNavigationViewItem;
            if (item == null)
            {
                return;
            }

            if (item.Items.Count == 0)
            {
                return;
            }

            if (item.IsExpanded && !item.IsSelected)
            {
                // The nav view is going to toggle the expanded state, so in the case where a user is returning to a top level menu that is already expanded, set the isexpanded property to false, so that the toggle that comes next returns the value to true.
                item.IsExpanded = false; 
                e.Handled = true;
            }
        }
    }
}