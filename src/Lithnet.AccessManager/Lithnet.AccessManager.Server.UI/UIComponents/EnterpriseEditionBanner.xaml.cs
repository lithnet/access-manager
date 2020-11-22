using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Lithnet.AccessManager.Server.UI.Providers;

namespace Lithnet.AccessManager.Server.UI
{
    public partial class EnterpriseEditionBanner : UserControl
    {
        public EnterpriseEditionBanner()
        {
            InitializeComponent();
        }

        public string FeatureName
        {
            get => (string)GetValue(FeatureNameProperty);
            set => SetValue(FeatureNameProperty, value);
        }

        public static readonly DependencyProperty FeatureNameProperty = DependencyProperty.Register(nameof(FeatureName), typeof(string), typeof(EnterpriseEditionBanner), new PropertyMetadata());

        public static readonly RoutedEvent LinkClickEvent = EventManager.RegisterRoutedEvent("LinkClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EnterpriseEditionBanner));

        public event RoutedEventHandler LinkClick
        {
            add { AddHandler(LinkClickEvent, value); }
            remove { RemoveHandler(LinkClickEvent, value); }
        }

        void RaiseClickEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(EnterpriseEditionBanner.LinkClickEvent);
            RaiseEvent(newEventArgs);
        }

        void OnClick()
        {
            RaiseClickEvent();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            this.RaiseClickEvent();
        }
    }
}
