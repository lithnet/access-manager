using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class HelpViewModel : PropertyChangedBase, IHaveDisplayName, IViewAware
    {
        public HelpViewModel()
        {
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public string DisplayName { get; set; } = "Help";

        public PackIconUniconsKind Icon => PackIconUniconsKind.QuestionCircle;
        
        public UIElement View { get; private set; }
    }
}
