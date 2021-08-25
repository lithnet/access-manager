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
    public class LicenseKeyViewModel : Screen, IExternalDialogAware, IHasSize
    {
        public LicenseKeyViewModel()
        {
            this.DisplayName = "Enter license Key";
        }

        public string LicenseKeyData { get; set; }

        public bool CancelButtonVisible { get; } = true;
        
        public bool SaveButtonVisible { get; } = true;
        
        public bool CancelButtonIsDefault { get; } = false;
        
        public bool SaveButtonIsDefault { get; } = true;
        
        public string SaveButtonName { get; } = "Save";
        
        public string CancelButtonName { get; } = "Cancel";
        
        public int Width { get; } = 500;
        
        public int Height { get; } = 600;
    }
}
