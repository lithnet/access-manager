using System;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;
using System.DirectoryServices.ActiveDirectory;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryMicrosoftLapsConfigurationViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IViewModelFactory<ActiveDirectoryForestSchemaViewModel, Forest> forestFactory;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly ILogger<ActiveDirectoryMicrosoftLapsConfigurationViewModel> logger;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IWindowManager windowManager;
        private readonly IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory;

        public ActiveDirectoryMicrosoftLapsConfigurationViewModel(IViewModelFactory<ActiveDirectoryForestSchemaViewModel, Forest> forestFactory, IDialogCoordinator dialogCoordinator, ILogger<ActiveDirectoryMicrosoftLapsConfigurationViewModel> logger, IShellExecuteProvider shellExecuteProvider, IDomainTrustProvider domainTrustProvider, IScriptTemplateProvider scriptTemplateProvider, IWindowsServiceProvider windowsServiceProvider, IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory, IWindowManager windowManager)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.forestFactory = forestFactory;
            this.shellExecuteProvider = shellExecuteProvider;
            this.domainTrustProvider = domainTrustProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.windowsServiceProvider = windowsServiceProvider;
            this.externalDialogWindowFactory = externalDialogWindowFactory;
            this.windowManager = windowManager;
            this.DisplayName = "Microsoft LAPS";

            this.Forests = new BindableCollection<ActiveDirectoryForestSchemaViewModel>();
        }
        public string HelpLink => Constants.HelpLinkPageMicrosoftLaps;

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
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }
        }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        private async Task PopulateForestsAndDomains()
        {
            try
            {
                foreach (Forest forest in this.domainTrustProvider.GetForests())
                {
                    this.Forests.Add(forestFactory.CreateViewModel(forest));
                }

                await this.RefreshData();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Unable to populate forest list");
            }
        }

        private async Task RefreshData()
        {
            foreach (var forest in this.Forests)
            {
                await forest.RefreshSchemaStatusAsync();
            }
        }

        public BindableCollection<ActiveDirectoryForestSchemaViewModel> Forests { get; }

        public ActiveDirectoryForestSchemaViewModel SelectedForest { get; set; }

        public async Task RefreshSchemaStatusAsync()
        {
            foreach (var vm in this.Forests)
            {
                await vm.RefreshSchemaStatusAsync();
            }
        }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }


        public async Task OpenMsLapsDownload()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkDownloadMsLaps);
        }

        public void DelegateMsLapsPermission()
        {
            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Modify the OU variable in this script, and run it with domain admin rights to assign permissions for the service account to be able to read Microsoft LAPS passwords from the directory",
                ScriptText = this.scriptTemplateProvider.GrantMsLapsPermissions.Replace("{serviceAccount}", this.windowsServiceProvider.GetServiceAccountSid().ToString(), StringComparison.OrdinalIgnoreCase)
            };

            var evm = this.externalDialogWindowFactory.CreateViewModel(vm);
            windowManager.ShowDialog(evm);
        }
    }
}
