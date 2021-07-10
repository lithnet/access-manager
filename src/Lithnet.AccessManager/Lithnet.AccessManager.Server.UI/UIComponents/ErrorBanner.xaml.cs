using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Lithnet.AccessManager.Server.UI.UIComponents
{
    /// <summary>
    /// Interaction logic for ErrorBanner.xaml
    /// </summary>
    public partial class ErrorBanner : UserControl
    {
        public ErrorBanner()
        {
            InitializeComponent();
        }

        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }

        public static readonly DependencyProperty MessageTextProperty = DependencyProperty.Register("MessageText", typeof(string), typeof(ErrorBanner), new PropertyMetadata(null, new PropertyChangedCallback(OnMessageTextChanged)));

        public string HeaderText
        {
            get { return (string)GetValue(BannerMessageProperty); }
            set { SetValue(BannerMessageProperty, value); }
        }

        public static readonly DependencyProperty BannerMessageProperty = DependencyProperty.Register("HeaderText", typeof(string), typeof(ErrorBanner), new PropertyMetadata());

        public Visibility ShowItem
        {
            get { return (Visibility)GetValue(ShowItemProperty); }
            set { SetValue(ShowItemProperty, value); }
        }

        public static readonly DependencyProperty ShowItemProperty = DependencyProperty.Register("ShowItem", typeof(Visibility), typeof(ErrorBanner), new PropertyMetadata(Visibility.Collapsed));

        private static void OnMessageTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ErrorBanner b)
            {
                if (e.NewValue == null)
                {
                    //b.Visibility = Visibility.Collapsed;
                   b.ShowItem = Visibility.Collapsed;

                }
                else
                {
                    //b.Visibility = Visibility.Visible;
                   b.ShowItem = Visibility.Visible;
                }
            }
        }

        public Task ShowMessage()
        {
            if (this.MessageText != null)
            {
                MessageBox.Show(this.MessageText, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }
    }
}
