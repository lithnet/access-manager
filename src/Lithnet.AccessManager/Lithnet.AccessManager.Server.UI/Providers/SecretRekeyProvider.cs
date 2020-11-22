using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    public class SecretRekeyProvider : ISecretRekeyProvider
    {
        private readonly EmailOptions emailOptions;
        private readonly AuthenticationOptions authnOptions;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IProtectedSecretProvider secretProvider;
        private readonly ILogger logger;

        public SecretRekeyProvider(IDialogCoordinator dialogCoordinator, ILogger<SecretRekeyProvider> logger, AuthenticationOptions authnOptions, EmailOptions emailOptions, IProtectedSecretProvider secretProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.authnOptions = authnOptions;
            this.emailOptions = emailOptions;
            this.secretProvider = secretProvider;
        }

        public async Task<bool> TryReKeySecretsAsync(object context)
        {
            ProtectedSecret oldEmailOptionsPassword = this.emailOptions.Password;
            ProtectedSecret oldOidcSecret = this.authnOptions.Oidc?.Secret;

            if (this.emailOptions.Password != null)
            {
                ProtectedSecret response = await this.TryReKeySecretsAsync(this.emailOptions.Password, "SMTP password", context);

                if (response == null)
                {
                    this.emailOptions.Password = oldEmailOptionsPassword;

                    return false;
                }

                this.emailOptions.Password = response;
            }

            if (this.authnOptions.Oidc?.Secret != null)
            {
                ProtectedSecret response = await this.TryReKeySecretsAsync(this.authnOptions.Oidc.Secret, "OpenID Connect secret", context);

                if (response == null)
                {
                    this.emailOptions.Password = oldEmailOptionsPassword;
                    this.authnOptions.Oidc.Secret = oldOidcSecret;

                    return false;
                }

                this.authnOptions.Oidc.Secret = response;
            }

            return true;
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

                LoginDialogData data = await this.dialogCoordinator.ShowLoginAsync(context, "Error", $"The {name} could not be automatically re-encrypted. Please re-enter the {name}",
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
