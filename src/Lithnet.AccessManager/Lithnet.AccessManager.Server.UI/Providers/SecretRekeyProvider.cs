using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    public class SecretRekeyProvider : ISecretRekeyProvider
    {
        private readonly EmailOptions emailOptions;
        private readonly AzureAdOptions azureAdOptions;
        private readonly AuthenticationOptions authnOptions;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IProtectedSecretProvider secretProvider;
        private readonly ILogger logger;

        public SecretRekeyProvider(IDialogCoordinator dialogCoordinator, ILogger<SecretRekeyProvider> logger, AuthenticationOptions authnOptions, EmailOptions emailOptions, IProtectedSecretProvider secretProvider, AzureAdOptions azureAdOptions)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.authnOptions = authnOptions;
            this.emailOptions = emailOptions;
            this.secretProvider = secretProvider;
            this.azureAdOptions = azureAdOptions;
        }

        public async Task<bool> TryReKeySecretsAsync(object context)
        {
            List<Action> rollbackActions = new List<Action>();

            if (this.emailOptions.Password != null)
            {
                ProtectedSecret oldEmailOptionsPassword = this.emailOptions.Password;
                rollbackActions.Add(() => this.emailOptions.Password = oldEmailOptionsPassword);

                ProtectedSecret response = await this.TryReKeySecretsAsync(this.emailOptions.Password, "SMTP password", context);

                if (response == null)
                {
                    this.InvokeRollbackActions(rollbackActions);
                    return false;
                }

                this.emailOptions.Password = response;
            }

            if (this.authnOptions.Oidc?.Secret != null)
            {
                ProtectedSecret oldOidcSecret = this.authnOptions.Oidc?.Secret;
                rollbackActions.Add(() => this.authnOptions.Oidc.Secret = oldOidcSecret);

                ProtectedSecret response = await this.TryReKeySecretsAsync(this.authnOptions.Oidc.Secret, "OpenID Connect secret", context);

                if (response == null)
                {
                    this.InvokeRollbackActions(rollbackActions);
                    return false;
                }

                this.authnOptions.Oidc.Secret = response;
            }

            foreach (var tenant in this.azureAdOptions.Tenants)
            {
                ProtectedSecret oldTenantSecret = tenant.ClientSecret;
                rollbackActions.Add(() => tenant.ClientSecret = oldTenantSecret);

                ProtectedSecret response = await this.TryReKeySecretsAsync(tenant.ClientSecret, $"Client secret for Azure AD client {tenant.ClientId} in tenant {tenant.TenantName} (ID {tenant.TenantId})", context);

                if (response == null)
                {
                    this.InvokeRollbackActions(rollbackActions);
                    return false;
                }

                tenant.ClientSecret = response;
            }

            return true;
        }

        private void InvokeRollbackActions(List<Action> rollbackActions)
        {
            foreach (var action in rollbackActions)
            {
                action();
            }
        }

        private async Task<ProtectedSecret> TryReKeySecretsAsync(ProtectedSecret secret, string name, object context)
        {
            try
            {
                string rawEmail = this.secretProvider.UnprotectSecret(secret);
                return this.secretProvider.ProtectSecret(rawEmail);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, $"Unable to re-encrypt {name}");

                LoginDialogData data = await this.dialogCoordinator.ShowLoginAsync(context, "Error", $"A protected secret could not be automatically re-encrypted. Please re-enter the {name}",
                                                                                   new LoginDialogSettings
                                                                                   {
                                                                                       ShouldHideUsername = true,
                                                                                       RememberCheckBoxVisibility = System.Windows.Visibility.Hidden,
                                                                                       AffirmativeButtonText = "OK",
                                                                                       NegativeButtonVisibility = System.Windows.Visibility.Visible,
                                                                                       NegativeButtonText = "Cancel"
                                                                                   });

                if (data != null)
                {
                    return this.secretProvider.ProtectSecret(data.Password);
                }
            }

            return null;
        }
    }
}
