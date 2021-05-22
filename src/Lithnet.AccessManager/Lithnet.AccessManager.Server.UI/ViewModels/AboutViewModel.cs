using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AboutViewModel : Screen
    {
        public AboutViewModel()
        {
            this.ThirdPartyNotices = new FlowDocument();

            TextRange range = new TextRange(this.ThirdPartyNotices.ContentStart, this.ThirdPartyNotices.ContentEnd);
            range.Load(EmbeddedResourceProvider.GetResourceStream("ThirdPartyNotices.rtf"), DataFormats.Rtf);

            this.CurrentVersion = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "Unknown version";
        }

        public FlowDocument ThirdPartyNotices { get; }

        public string CurrentVersion { get; }
    }
}
