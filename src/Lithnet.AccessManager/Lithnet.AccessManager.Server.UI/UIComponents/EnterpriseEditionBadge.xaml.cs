using System.Drawing;
using System.Management.Automation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    public partial class EnterpriseEditionBadge : UserControl
    {
        public EnterpriseEditionBadge()
        {
            InitializeComponent();
        }

        public string ToolTipText
        {
            get => (string)GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool IsSolid
        {
            get => (bool)GetValue(IsSolidProperty);
            set => SetValue(IsSolidProperty, value);
        }

        public bool ShowText
        {
            get => (bool)GetValue(ShowTextProperty);
            set => SetValue(ShowTextProperty, value);
        }


        public static readonly DependencyProperty ToolTipTextProperty = DependencyProperty.Register(nameof(ToolTipText), typeof(string), typeof(EnterpriseEditionBadge), new PropertyMetadata());

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(EnterpriseEditionBadge), new PropertyMetadata("Enterprise edition"));

        public static readonly DependencyProperty IsSolidProperty = DependencyProperty.Register(nameof(IsSolid), typeof(bool), typeof(EnterpriseEditionBadge), new PropertyMetadata());
        
        public static readonly DependencyProperty ShowTextProperty = DependencyProperty.Register(nameof(ShowText), typeof(bool), typeof(EnterpriseEditionBadge), new PropertyMetadata(true));
    }
}
