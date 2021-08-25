using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IViewModelFactory<ActiveDirectoryDomainPermissionViewModel, Domain> domainFactory;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly ILogger<ActiveDirectoryConfigurationViewModel> logger;
        private readonly IWindowManager windowManager;
        private readonly IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory;

        public PackIconBoxIconsKind Icon => PackIconBoxIconsKind.RegularBookContent;

        public ActiveDirectoryConfigurationViewModel(ActiveDirectoryMicrosoftLapsConfigurationViewModel msLapsVm, ActiveDirectoryLithnetLapsConfigurationViewModel lithnetLapsVm, ActiveDirectoryBitLockerViewModel bitLockerVm, ActiveDirectoryJitConfigurationViewModel jitVm, IViewModelFactory<ActiveDirectoryDomainPermissionViewModel, Domain> domainFactory, IDialogCoordinator dialogCoordinator, IWindowsServiceProvider windowsServiceProvider, ILogger<ActiveDirectoryConfigurationViewModel> logger, IShellExecuteProvider shellExecuteProvider, IDomainTrustProvider domainTrustProvider, IScriptTemplateProvider scriptTemplateProvider, IWindowManager windowManager, IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.domainFactory = domainFactory;
            this.windowsServiceProvider = windowsServiceProvider;
            this.logger = logger;
            this.shellExecuteProvider = shellExecuteProvider;
            this.domainTrustProvider = domainTrustProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.windowManager = windowManager;
            this.externalDialogWindowFactory = externalDialogWindowFactory;
            this.DisplayName = "Active Directory";

            this.Items.Add(msLapsVm);
            this.Items.Add(lithnetLapsVm);
            this.Items.Add(bitLockerVm);
            this.Items.Add(jitVm);

            this.Domains = new BindableCollection<ActiveDirectoryDomainPermissionViewModel>();
        }

        public string HelpLink => Constants.HelpLinkPageActiveDirectory;

        protected override void OnInitialActivate()
        {
            Task.Run(async () => await this.Initialize());
        }

        private async Task Initialize()
        {
            try
            {
                await this.PopulateForestsAndDomains();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not populate the forest list");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "Could not initialize the forest list";
            }
        }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        private async Task PopulateForestsAndDomains()
        {
            foreach (Forest forest in this.domainTrustProvider.GetForests())
            {
                foreach (Domain domain in this.domainTrustProvider.GetDomains(forest))
                {
                    this.Domains.Add(domainFactory.CreateViewModel(domain));
                }
            }

            await this.RefreshData();
        }

        private async Task RefreshData()
        {
            foreach (var domain in this.Domains)
            {
                await domain.RefreshGroupMembershipAsync();
            }
        }

        public BindableCollection<ActiveDirectoryDomainPermissionViewModel> Domains { get; }

        public ActiveDirectoryDomainPermissionViewModel SelectedDomain { get; set; }

        public async Task RefreshGroupMembershipAsync()
        {
            try
            {
                foreach (var vm in this.Domains)
                {
                    await vm.RefreshGroupMembershipAsync();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanShowADPermissionScript => this.SelectedDomain != null;

        public async Task ShowADPermissionScript()
        {
            try
            {
                var current = this.SelectedDomain;

                if (current == null)
                {
                    return;
                }

                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Run the following script with Domain Admins rights to add the service account to the correct groups",
                    ScriptText = this.scriptTemplateProvider.AddDomainMembershipPermissions
                        .Replace("{domainDNS}", current.Name, StringComparison.OrdinalIgnoreCase)
                        .Replace("{serviceAccountSid}", this.windowsServiceProvider.GetServiceAccountSid().Value, StringComparison.OrdinalIgnoreCase)
                };

                var evm = this.externalDialogWindowFactory.CreateViewModel(vm);
                windowManager.ShowDialog(evm);

                await current.RefreshGroupMembershipAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
