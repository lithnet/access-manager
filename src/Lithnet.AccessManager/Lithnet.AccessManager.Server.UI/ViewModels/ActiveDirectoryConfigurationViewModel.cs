using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryConfigurationViewModel : Screen, IHaveDisplayName
    {
        private readonly IDialogCoordinator dialogCoordinator;

        private readonly IActiveDirectoryDomainPermissionViewModelFactory domainFactory;

        private readonly IActiveDirectoryForestSchemaViewModelFactory forestFactory;

        private readonly IServiceSettingsProvider serviceSettings;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;

        public ActiveDirectoryConfigurationViewModel(IActiveDirectoryForestSchemaViewModelFactory forestFactory, IActiveDirectoryDomainPermissionViewModelFactory domainFactory,  IDialogCoordinator dialogCoordinator, IServiceSettingsProvider serviceSettings)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.domainFactory = domainFactory;
            this.forestFactory = forestFactory;
            this.serviceSettings = serviceSettings;
            this.DisplayName = "Active Directory";

            this.Forests = new List<ActiveDirectoryForestSchemaViewModel>();
            this.Domains = new List<ActiveDirectoryDomainPermissionViewModel>();
        }

        protected override void OnActivate()
        {
            this.PopulateForestsAndDomains();
            //this.SelectedForest = this.Forests.FirstOrDefault();
        }

        private void PopulateForestsAndDomains()
        {
            this.Forests.Clear();
            this.Domains.Clear();

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
        }

        public List<ActiveDirectoryForestSchemaViewModel> Forests { get;  }

        public ActiveDirectoryForestSchemaViewModel SelectedForest { get; set; }

        public List<ActiveDirectoryDomainPermissionViewModel> Domains { get;}

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
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();

            await Task.Run(() => current.RefreshSchemaStatus()).ConfigureAwait(false);
        }

        public void RefreshSchemaStatus()
        {
            foreach (var vm in this.Forests)
            {
                Task.Run(() => vm.RefreshSchemaStatus());
            }
        }

        public void RefreshGroupMembership()
        {
            foreach (var vm in this.Domains)
            {
                Task.Run(() => vm.RefreshGroupMembership());
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
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();

            current.RefreshGroupMembership();
        }
    }
}
