using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        public AmsDirectoryConfigurationViewModel(AmsDirectoryRegistrationKeysViewModel registrationKeysVm, AmsDirectoryDevicesViewModel devicesVm, AmsDirectoryLithnetLapsConfigurationViewModel lapsVm)
        {
            this.Items.Add(registrationKeysVm);
            this.Items.Add(devicesVm);
            this.Items.Add(lapsVm);
            this.DisplayName = "Access Manager Directory";
        }

        public string HelpLink => Constants.HelpLinkPageWebHosting;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;
    }
}