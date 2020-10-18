using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(TextBox), Control.MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText));

            EventManager.RegisterClassHandler(typeof(PasswordBox), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(PasswordBox), UIElement.GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(PasswordBox), Control.MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText));
            base.OnStartup(e);
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
