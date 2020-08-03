using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using Microsoft.Xaml.Behaviors;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Fixes an issue where the list view steals the mouse wheel events
    /// </summary>
    public class ListViewScrollBarBehavior : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += ListView_PreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseWheel -= ListView_PreviewMouseWheel;
        }

        private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                ListView lv = sender as ListView;
                ScrollViewer scrollViewer = lv.FindChild<ScrollViewer>();

                if (scrollViewer.ComputedVerticalScrollBarVisibility != Visibility.Visible)
                {
                    PassOnMouseWheelEvent(lv, e);
                }
                else
                {
                    if (Math.Abs(scrollViewer.VerticalOffset) <= 0.1)
                    {
                        if (e.Delta > 0)
                        { 
                            PassOnMouseWheelEvent(lv, e);
                        }
                    }
                    else if (Math.Abs(scrollViewer.VerticalOffset) >= scrollViewer.ScrollableHeight)
                    {
                        if (e.Delta < 0)
                        {
                            PassOnMouseWheelEvent(lv, e);
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void PassOnMouseWheelEvent(ListView lv, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            e2.RoutedEvent = UIElement.MouseWheelEvent;
            lv.RaiseEvent(e2);
        }
    }
}
