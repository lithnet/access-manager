﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.UI.Providers;
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
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;

        public ActiveDirectoryConfigurationViewModel(IActiveDirectoryForestSchemaViewModelFactory forestFactory, IActiveDirectoryDomainPermissionViewModelFactory domainFactory, IDialogCoordinator dialogCoordinator, IWindowsServiceProvider windowsServiceProvider, ILogger<ActiveDirectoryConfigurationView> logger, IShellExecuteProvider shellExecuteProvider, IDomainTrustProvider domainTrustProvider, IScriptTemplateProvider scriptTemplateProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.domainFactory = domainFactory;
            this.forestFactory = forestFactory;
            this.windowsServiceProvider = windowsServiceProvider;
            this.shellExecuteProvider = shellExecuteProvider;
            this.domainTrustProvider = domainTrustProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.DisplayName = "Active Directory";

            this.Forests = new BindableCollection<ActiveDirectoryForestSchemaViewModel>();
            this.Domains = new BindableCollection<ActiveDirectoryDomainPermissionViewModel>();
        }

        public string HelpLink => Constants.HelpLinkPageActiveDirectory;

        private Task initialize;

        protected override void OnInitialActivate()
        {
            this.initialize = Task.Run(async () => await this.PopulateForestsAndDomains());
        }

        private async Task PopulateForestsAndDomains()
        {
            foreach (Forest forest in this.domainTrustProvider.GetForests())
            {
                this.Forests.Add(forestFactory.CreateViewModel(forest));

                foreach (Domain domain in this.domainTrustProvider.GetDomains(forest))
                {
                    this.Domains.Add(domainFactory.CreateViewModel(domain));
                }
            }

            await this.RefreshData();
        }

        private async Task RefreshData()
        {
            foreach (var forest in this.Forests)
            {
                await forest.RefreshSchemaStatusAsync();
            }

            foreach (var domain in this.Domains)
            {
                await domain.RefreshGroupMembershipAsync();
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
                ScriptText = this.scriptTemplateProvider.UpdateAdSchema
                    .Replace("{forest}", current.Name)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
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
                ScriptText = this.scriptTemplateProvider.AddDomainMembershipPermissions
                    .Replace("{domainDNS}", current.Name, StringComparison.OrdinalIgnoreCase)
                    .Replace("{serviceAccountSid}", this.windowsServiceProvider.GetServiceAccountSid().Value, StringComparison.OrdinalIgnoreCase)
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

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
