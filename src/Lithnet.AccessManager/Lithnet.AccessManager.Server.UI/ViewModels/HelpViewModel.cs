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
    public class HelpViewModel : Screen
    {
        public HelpViewModel()
        {
            this.DisplayName = "Help";
        }

        public PackIconUniconsKind Icon => PackIconUniconsKind.QuestionCircle;
    }
}
