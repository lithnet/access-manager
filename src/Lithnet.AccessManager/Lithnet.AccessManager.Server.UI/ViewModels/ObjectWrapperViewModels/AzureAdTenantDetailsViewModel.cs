using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Api;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class AzureAdTenantDetailsViewModel : ValidatingModelBase, IViewAware
    {
        private readonly ILogger<AzureAdTenantDetailsViewModel> logger;
        private readonly IProtectedSecretProvider secretProvider;

        public AzureAdTenantDetails Model { get; }

        public UIElement View { get; set; }

        public AzureAdTenantDetailsViewModel(AzureAdTenantDetails model, ILogger<AzureAdTenantDetailsViewModel> logger, IModelValidator<AzureAdTenantDetailsViewModel> validator, IProtectedSecretProvider secretProvider)
        {
            this.logger = logger;
            this.secretProvider = secretProvider;
            this.Model = model;
            this.Validator = validator;
            this.Validate();
        }

        public string TenantId
        {
            get => this.Model.TenantId;
            set => this.Model.TenantId = value;
        }

        public string ClientId
        {
            get => this.Model.ClientId;
            set => this.Model.ClientId = value;
        }

        public string TenantName
        {
            get => this.Model.TenantName;
            set => this.Model.TenantName = value;
        }

        public string ClientSecret
        {
            get => this.Model.ClientSecret?.Data == null ? null : "-placeholder-";
            set
            {
                if (value != "-placeholder-")
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        this.Model.ClientSecret = null;
                        return;
                    }

                    this.Model.ClientSecret = this.secretProvider.ProtectSecret(value);
                }
            }
        }


        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}
