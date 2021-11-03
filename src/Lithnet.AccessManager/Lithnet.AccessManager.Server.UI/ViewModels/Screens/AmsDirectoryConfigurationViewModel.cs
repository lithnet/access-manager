using System.Threading.Tasks;
using Lithnet.AccessManager.Api;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        private readonly ApiAuthenticationOptions agentOptions;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AmsDirectoryConfigurationViewModel(AmsDirectoryRegistrationKeysViewModel registrationKeysVm, AmsDirectoryDevicesViewModel devicesVm, AmsDirectoryGroupsViewModel groupsVm, AmsDirectoryLithnetLapsConfigurationViewModel lapsVm, ApiAuthenticationOptions agentOptions, INotifyModelChangedEventPublisher eventPublisher, IViewModelFactory<EnterpriseEditionBannerViewModel, EnterpriseEditionBannerModel> enterpriseEditionViewModelFactory, IShellExecuteProvider shellExecuteProvider)
        {
            this.agentOptions = agentOptions;
            this.shellExecuteProvider = shellExecuteProvider;

            this.Items.Add(devicesVm);
            this.Items.Add(groupsVm);
            this.Items.Add(registrationKeysVm);
            this.Items.Add(lapsVm);

            this.DisplayName = "Access Manager Directory";
            eventPublisher.Register(this);

            this.EnterpriseEdition = enterpriseEditionViewModelFactory.CreateViewModel(new EnterpriseEditionBannerModel
            {
                RequiredFeature = Enterprise.LicensedFeatures.AmsRegisteredDeviceSupport,
                Link = Constants.EnterpriseEditionLearnMoreLinkAmsDevices
            });
        }

        public EnterpriseEditionBannerViewModel EnterpriseEdition { get; set; }

        public string HelpLink => Constants.HelpLinkAmsDirectoryConfigPage;

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool AllowAmsManagedDeviceAuth { get => this.agentOptions.AllowAmsManagedDeviceAuth; set => this.agentOptions.AllowAmsManagedDeviceAuth = value; }



        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool AllowAzureAdJoinedDevices
        {
            get => this.agentOptions.AllowAzureAdJoinedDeviceAuth;
            set => this.agentOptions.AllowAzureAdJoinedDeviceAuth = value;
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool AllowAzureAdRegisteredDevices
        {
            get => this.agentOptions.AllowAzureAdRegisteredDeviceAuth;
            set => this.agentOptions.AllowAzureAdRegisteredDeviceAuth = value;
        }


        public PackIconMaterialKind Icon => PackIconMaterialKind.ShieldLock;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}