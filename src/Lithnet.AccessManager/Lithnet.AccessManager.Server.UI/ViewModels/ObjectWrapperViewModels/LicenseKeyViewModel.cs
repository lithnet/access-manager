using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class LicenseKeyViewModel : Screen
    {
        public LicenseKeyViewModel()
        {
        }

        public string LicenseKeyData { get; set; }
    }
}
