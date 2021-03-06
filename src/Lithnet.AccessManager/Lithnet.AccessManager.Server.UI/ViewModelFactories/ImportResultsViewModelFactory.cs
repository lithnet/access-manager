﻿using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ImportResultsViewModelFactory : IImportResultsViewModelFactory
    {
        private readonly ISecurityDescriptorTargetsViewModelFactory targetsFactory;
        private readonly Func<IEventAggregator> eventAggregator;
        private readonly ILogger<ImportResultsViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public ImportResultsViewModelFactory(IDialogCoordinator dialogCoordinator, ILogger<ImportResultsViewModel> logger, Func<IEventAggregator> eventAggregator, ISecurityDescriptorTargetsViewModelFactory targetsFactory, IShellExecuteProvider shellExecuteProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.eventAggregator = eventAggregator;
            this.targetsFactory = targetsFactory;
            this.shellExecuteProvider = shellExecuteProvider;
        }

        public async Task<ImportResultsViewModel> CreateViewModelAsync(ImportResults model)
        {
            var item = new ImportResultsViewModel(model, this.targetsFactory, eventAggregator.Invoke(), logger, dialogCoordinator, shellExecuteProvider);
            await item.Initialization;
            return item;
        }
    }
}
