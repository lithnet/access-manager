﻿using System;
using Lithnet.AccessManager.Api;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PasswordPolicyViewModelFactory : IViewModelFactory<PasswordPolicyViewModel, PasswordPolicyEntry>
    {
        private readonly Func<IModelValidator<PasswordPolicyViewModel>> validator;
        private readonly IViewModelFactory<AzureAdObjectSelectorViewModel> aadSelectorFactory;
        private readonly IViewModelFactory<SelectTargetTypeViewModel> selectTargetTypeFactory;
        private readonly IViewModelFactory<AmsGroupSelectorViewModel> amsSelectorFactory;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<PasswordPolicyViewModel> logger;

        public PasswordPolicyViewModelFactory(Func<IModelValidator<PasswordPolicyViewModel>> validator, IViewModelFactory<AzureAdObjectSelectorViewModel> aadSelectorFactory, IViewModelFactory<SelectTargetTypeViewModel> selectTargetTypeFactory, IViewModelFactory<AmsGroupSelectorViewModel> amsSelectorFactory, IDialogCoordinator dialogCoordinator, ILogger<PasswordPolicyViewModel> logger)
        {
            this.validator = validator;
            this.aadSelectorFactory = aadSelectorFactory;
            this.selectTargetTypeFactory = selectTargetTypeFactory;
            this.amsSelectorFactory = amsSelectorFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
        }

        public PasswordPolicyViewModel CreateViewModel(PasswordPolicyEntry model)
        {
            return new PasswordPolicyViewModel(model, validator.Invoke(), aadSelectorFactory, selectTargetTypeFactory, amsSelectorFactory, dialogCoordinator, logger);
        }
    }
}