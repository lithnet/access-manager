using Stylet;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace Lithnet.AccessManager.Server.UI
{
    public class AboutViewModel : Screen, IExternalDialogAware, IHasSize
    {
        public AboutViewModel()
        {
            this.ThirdPartyNotices = new FlowDocument();

            TextRange range = new TextRange(this.ThirdPartyNotices.ContentStart, this.ThirdPartyNotices.ContentEnd);
            range.Load(EmbeddedResourceProvider.GetResourceStream("ThirdPartyNotices.rtf"), DataFormats.Rtf);

            this.CurrentVersion = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "Unknown version";
            this.DisplayName = "About Lithnet Access Manager";
        }

        public FlowDocument ThirdPartyNotices { get; }

        public string CurrentVersion { get; }

        public bool CancelButtonVisible { get; } = true;

        public bool SaveButtonVisible { get; } = false;

        public bool CancelButtonIsDefault { get; } = true;

        public bool SaveButtonIsDefault { get; } = false;

        public string SaveButtonName { get; } = null;

        public string CancelButtonName { get; } = "Close";

        public int Width { get; } = 1000;

        public int Height { get; } = 600;
    }
}