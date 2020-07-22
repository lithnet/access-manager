using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for NotificationChannelSelectionView.xaml
    /// </summary>
    public partial class NotificationChannelSelectionView : UserControl
    {
        public NotificationChannelSelectionView()
        {
            InitializeComponent();
        }

        private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                ListView lv = sender as ListView;
                ScrollViewer scrollViewer = lv.FindChild<ScrollViewer>();

                if (scrollViewer.ComputedVerticalScrollBarVisibility != Visibility.Visible)
                {
                    PassOnMouseWheelEvent(e);
                }
                else
                {
                    if (Math.Abs(scrollViewer.VerticalOffset) <= 0.1)
                    {
                        if (e.Delta > 0)
                        {
                            PassOnMouseWheelEvent(e);
                        }
                    }
                    else if (Math.Abs(scrollViewer.VerticalOffset) >= scrollViewer.ScrollableHeight)
                    {
                        if (e.Delta < 0)
                        {
                            PassOnMouseWheelEvent(e);
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void PassOnMouseWheelEvent(MouseWheelEventArgs e)
        {
            e.Handled = true;
            var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            e2.RoutedEvent = UIElement.MouseWheelEvent;
            this.RaiseEvent(e2);
        }
    }
}
