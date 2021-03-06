﻿using System.DirectoryServices.ActiveDirectory;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class JitDomainStatusViewModelFactory : IJitDomainStatusViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IDirectory directory;
        private readonly ILogger<JitDomainStatusViewModel> logger;

        public JitDomainStatusViewModelFactory(IDialogCoordinator dialogCoordinator, IDirectory directory, ILogger<JitDomainStatusViewModel> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.directory = directory;
            this.logger = logger;
        }

        public JitDomainStatusViewModel CreateViewModel(Domain model, JitDynamicGroupMapping mapping)
        {
            return new JitDomainStatusViewModel(directory, mapping, model, logger);
        }
    }
}
