using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Markdig.Extensions.TaskLists;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryConfigurationViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;

        private readonly IActiveDirectoryDomainPermissionViewModelFactory domainFactory;

        private readonly IActiveDirectoryForestSchemaViewModelFactory forestFactory;

        private readonly IServiceSettingsProvider serviceSettings;

        private readonly ILogger<ActiveDirectoryConfigurationView> logger;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;

        public ActiveDirectoryConfigurationViewModel(IActiveDirectoryForestSchemaViewModelFactory forestFactory, IActiveDirectoryDomainPermissionViewModelFactory domainFactory, IDialogCoordinator dialogCoordinator, IServiceSettingsProvider serviceSettings, ILogger<ActiveDirectoryConfigurationView> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.domainFactory = domainFactory;
            this.forestFactory = forestFactory;
            this.serviceSettings = serviceSettings;
            this.logger = logger;
            this.DisplayName = "Active Directory";

            this.Forests = new BindableCollection<ActiveDirectoryForestSchemaViewModel>();
            this.Domains = new BindableCollection<ActiveDirectoryDomainPermissionViewModel>();
        }

        public string HelpLink => Constants.HelpLinkPageActiveDirectory;

        protected override void OnInitialActivate()
        {
            Task.Run(this.PopulateForestsAndDomains);
        }

        private void PopulateForestsAndDomains()
        {
            var currentDomain = Domain.GetCurrentDomain();
            this.Forests.Add(forestFactory.CreateViewModel(currentDomain.Forest));

            foreach (var domain in currentDomain.Forest.Domains.OfType<Domain>())
            {
                this.Domains.Add(domainFactory.CreateViewModel(domain));
            }

            foreach (var trust in currentDomain.Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    var forest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, trust.TargetName));
                    this.Forests.Add(forestFactory.CreateViewModel(forest));

                    foreach (var domain in forest.Domains.OfType<Domain>())
                    {
                        this.Domains.Add(domainFactory.CreateViewModel(domain));
                    }
                }
            }

            this.RefreshData();
        }

        private void RefreshData()
        {
            foreach (var forest in this.Forests)
            {
                _ = forest.RefreshSchemaStatusAsync();
            }

            foreach (var domain in this.Domains)
            {
                _ = domain.RefreshGroupMembershipAsync();
            }
        }

        public BindableCollection<ActiveDirectoryForestSchemaViewModel> Forests { get; }

        public ActiveDirectoryForestSchemaViewModel SelectedForest { get; set; }

        public BindableCollection<ActiveDirectoryDomainPermissionViewModel> Domains { get; }

        public ActiveDirectoryDomainPermissionViewModel SelectedDomain { get; set; }

        public bool CanExtendSchemaLithnetAccessManager => this.SelectedForest?.IsNotLithnetSchemaPresent == true;

        public async Task ExtendSchemaLithnetAccessManager()
        {
            ActiveDirectoryForestSchemaViewModel current = this.SelectedForest;

            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run the following script as an account that is a member of the 'Schema Admins' group",
                ScriptText = ScriptTemplates.UpdateAdSchemaTemplate
                    .Replace("{forest}", current.Name)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Width = 650,
                Height = 400,
                Title = "Script",
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();

            await Task.Run(() => current.RefreshSchemaStatus()).ConfigureAwait(false);
        }

        public async Task RefreshSchemaStatusAsync()
        {
            foreach (var vm in this.Forests)
            {
                await vm.RefreshSchemaStatusAsync();
            }
        }

        public async Task RefreshGroupMembershipAsync()
        {
            foreach (var vm in this.Domains)
            {
                await vm.RefreshGroupMembershipAsync();
            }
        }

        public bool CanShowADPermissionScript => this.SelectedDomain != null;

        public void ShowADPermissionScript()
        {
            var current = this.SelectedDomain;

            if (current == null)
            {
                return;
            }

            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run the following script with Domain Admins rights to add the service account to the correct groups",
                ScriptText = ScriptTemplates.AddDomainGroupMembershipPermissions
                    .Replace("{domainDNS}", current.Name, StringComparison.OrdinalIgnoreCase)
                    .Replace("{serviceAccountSid}", this.serviceSettings.GetServiceAccount().Value, StringComparison.OrdinalIgnoreCase)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Title = "Script",
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();

            current.RefreshGroupMembership();
        }
    }
}
