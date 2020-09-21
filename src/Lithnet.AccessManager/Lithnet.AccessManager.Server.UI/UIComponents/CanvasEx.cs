using System.Windows;
using System.Windows.Controls;

namespace Lithnet.AccessManager.Server.UI.Views
{
    public class CanvasEx : Canvas
    {
        public CanvasEx()
        {
            this.SizeChanged += CanvasEx_SizeChanged;
        }

        void CanvasEx_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BindableActualHeight = this.ActualHeight;
            BindableActualWidth = this.ActualWidth;
        }

        public double BindableActualWidth
        {
            get => (double)GetValue(BindableActualWidthProperty);
            set => SetValue(BindableActualWidthProperty, value);
        }

        public static readonly DependencyProperty BindableActualWidthProperty = DependencyProperty.Register("BindableActualWidth", typeof(double), typeof(CanvasEx), new PropertyMetadata(0d));

        public double BindableActualHeight
        {
            get => (double)GetValue(BindableActualHeightProperty);
            set => SetValue(BindableActualHeightProperty, value);
        }

        public static readonly DependencyProperty BindableActualHeightProperty = DependencyProperty.Register("BindableActualHeight", typeof(double), typeof(CanvasEx), new PropertyMetadata(0d));
    }
}
