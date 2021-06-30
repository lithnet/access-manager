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
        private readonly IActiveDirectoryForestSchemaViewModelFactory forestFactory;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly ILogger<ActiveDirectoryMicrosoftLapsConfigurationViewModel> logger;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        //public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;

        public ActiveDirectoryMicrosoftLapsConfigurationViewModel(IActiveDirectoryForestSchemaViewModelFactory forestFactory, IDialogCoordinator dialogCoordinator, ILogger<ActiveDirectoryMicrosoftLapsConfigurationViewModel> logger, IShellExecuteProvider shellExecuteProvider, IDomainTrustProvider domainTrustProvider, IScriptTemplateProvider scriptTemplateProvider, IWindowsServiceProvider windowsServiceProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.forestFactory = forestFactory;
            this.shellExecuteProvider = shellExecuteProvider;
            this.domainTrustProvider = domainTrustProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.windowsServiceProvider = windowsServiceProvider;
            this.DisplayName = "Microsoft LAPS";

            this.Forests = new BindableCollection<ActiveDirectoryForestSchemaViewModel>();
        }
        public string HelpLink => Constants.HelpLinkPageActiveDirectory;

        private Task initialize;

        protected override void OnInitialActivate()
        {
            this.initialize = Task.Run(async () => await this.PopulateForestsAndDomains());
        }

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

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Title = "Script",
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();
        }
    }
}
