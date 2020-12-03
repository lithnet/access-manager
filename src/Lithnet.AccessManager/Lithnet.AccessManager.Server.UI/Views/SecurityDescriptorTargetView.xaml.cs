using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Lithnet.AccessManager.Server.UI
{
    /// <summary>
    /// Interaction logic for SecurityDescriptorTargetsView.xaml
    /// </summary>
    public partial class SecurityDescriptorTargetView : UserControl
    {
        public SecurityDescriptorTargetView()
        {
            InitializeComponent();
        }

        private void HandleExpanderExpanded(object sender, RoutedEventArgs e)
        {
            ExpandExculsively(sender as Expander);
        }

        private void ExpandExculsively(Expander expander)
        {
            foreach (var child in ((Panel)expander.Parent).Children)
            {
                if (child is Expander && child != expander)
                    ((Expander)child).IsExpanded = false;
            }
        }
    }
}
