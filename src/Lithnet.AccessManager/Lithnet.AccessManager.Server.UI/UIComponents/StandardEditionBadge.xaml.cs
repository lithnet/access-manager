using System.Windows;
using System.Windows.Controls;

namespace Lithnet.AccessManager.Server.UI
{
    public partial class StandardEditionBadge : UserControl
    {
        public StandardEditionBadge()
        {
            InitializeComponent();
        }

        public string ToolTipText
        {
            get => (string)GetValue(ToolTipTextProperty);
            set => SetValue(ToolTipTextProperty, value);
        }

       
        public bool IsSolid
        {
            get => (bool)GetValue(IsSolidProperty);
            set
            {

                SetValue(IsSolidProperty, value);
            }
        }

        public static readonly DependencyProperty ToolTipTextProperty = DependencyProperty.Register(nameof(ToolTipText), typeof(string), typeof(StandardEditionBadge), new PropertyMetadata());

        public static readonly DependencyProperty IsSolidProperty = DependencyProperty.Register(nameof(IsSolid), typeof(bool), typeof(StandardEditionBadge), new PropertyMetadata());
    }
}
