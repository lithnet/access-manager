using Lithnet.AccessManager.Api;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        private readonly ApiAuthenticationOptions agentOptions;

        public AmsDirectoryConfigurationViewModel(AmsDirectoryRegistrationKeysViewModel registrationKeysVm, AmsDirectoryDevicesViewModel devicesVm, AmsDirectoryLithnetLapsConfigurationViewModel lapsVm, ApiAuthenticationOptions agentOptions, INotifyModelChangedEventPublisher eventPublisher)
        {
            this.agentOptions = agentOptions;

            this.Items.Add(registrationKeysVm);
            this.Items.Add(devicesVm);
            this.Items.Add(lapsVm);

            this.DisplayName = "Access Manager Directory";
            eventPublisher.Register(this);
        }

        public string HelpLink => Constants.HelpLinkPageWebHosting;

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool AllowAmsManagedDeviceAuth { get => this.agentOptions.AllowAmsManagedDeviceAuth; set => this.agentOptions.AllowAmsManagedDeviceAuth = value; }

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;
    }
}