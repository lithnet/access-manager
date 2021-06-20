using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent
{
    public class AdvancedLapsAgent
    {
        private readonly ILogger<AdvancedLapsAgent> logger;
        private readonly ISettingsProvider settings;
        private readonly IPasswordGenerator passwordGenerator;
        private readonly IPasswordChangeProvider passwordChangeProvider;
        private readonly IPasswordStorageProvider passwordStorageProvider;
        private readonly IRegistrationProvider registrationProvider;
        private readonly IAgentCheckInProvider checkInProvider;

        public AdvancedLapsAgent(ILogger<AdvancedLapsAgent> logger, ISettingsProvider settings, IPasswordGenerator passwordGenerator, IPasswordChangeProvider passwordChangeProvider, IPasswordStorageProvider passwordStorageProvider, IRegistrationProvider registrationProvider, IAgentCheckInProvider checkInProvider)
        {
            this.logger = logger;
            this.settings = settings;
            this.passwordGenerator = passwordGenerator;
            this.passwordChangeProvider = passwordChangeProvider;
            this.passwordStorageProvider = passwordStorageProvider;
            this.registrationProvider = registrationProvider;
            this.checkInProvider = checkInProvider;
        }

        public async Task DoCheckAsync()
        {
            if (this.settings.AuthenticationMode == AgentAuthenticationMode.Ssa)
            {
                var state = await this.registrationProvider.GetRegistrationState();

                switch (state)
                {
                    case RegistrationState.Approved:
                        await this.CheckAndChangePassword();
                        break;

                    case RegistrationState.NotRegistered:
                        if (this.registrationProvider.CanRegisterAgent())
                        {
                            var result = await this.registrationProvider.RegisterAgent();

                            if (result == RegistrationState.Approved)
                            {
                                await this.CheckAndChangePassword();
                            }
                        }
                        else
                        {
                            this.logger.LogWarning("The client is not eligible to register itself. Please ensure the client has an active registration key");
                        }
                        break;


                    case RegistrationState.Pending:
                    case RegistrationState.Rejected:
                        return;
                }
            }
            else
            {
                await this.checkInProvider.CheckinIfRequired();
                await this.CheckAndChangePassword();
            }
        }

        private async Task CheckAndChangePassword()
        {
            try
            {
                if (await this.passwordStorageProvider.IsPasswordChangeRequired())
                {
                    this.logger.LogTrace(EventIDs.PasswordExpired, "Password has expired and needs to be changed");
                    await this.ChangePassword();
                }
                else
                {
                    this.logger.LogTrace(EventIDs.PasswordChangeNotRequired, "Password does not need to be changed");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsUnexpectedException, ex, "The LAPS agent process encountered an error");
            }
        }

        private async Task ChangePassword()
        {
            try
            {
                string newPassword = this.passwordGenerator.Generate();
                DateTime expiryDate = DateTime.UtcNow.AddDays(Math.Max(this.settings.MaximumPasswordAgeDays, 1));

                await this.passwordStorageProvider.UpdatePassword(this.passwordChangeProvider.GetAccountName(), newPassword, expiryDate);

                this.logger.LogTrace(EventIDs.SetPasswordOnAmAttribute, "Password successfully committed to storage");

                try
                {
                    this.passwordChangeProvider.ChangePassword(newPassword);
                    this.logger.LogInformation(EventIDs.SetPassword, "The local administrator password has been changed and will expire on {expiryDate}", expiryDate.ToLocalTime());
                }
                catch (Exception)
                {
                    await this.passwordStorageProvider.RollbackPasswordUpdate();
                    throw;
                }
                finally
                {
                    await this.passwordStorageProvider.Commit();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.PasswordChangeFailure, ex, "The password change operation failed");
            }
        }
    }
}
