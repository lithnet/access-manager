using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
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
        private readonly IMetadataProvider metadataProvider;
        private readonly ILithnetAdminPasswordProvider lithnetAdminPasswordProvider;
        private readonly IDirectory directory;
        private readonly ILocalSam sam;
        private readonly IMsMcsAdmPwdProvider msMcsPasswordProvider;

        public AdvancedLapsAgent(ILogger<AdvancedLapsAgent> logger, ISettingsProvider settings, IPasswordGenerator passwordGenerator, IPasswordChangeProvider passwordChangeProvider, IPasswordStorageProvider passwordStorageProvider, IRegistrationProvider registrationProvider, IAgentCheckInProvider checkInProvider, IMetadataProvider metadataProvider)
        {
            this.logger = logger;
            this.settings = settings;
            this.passwordGenerator = passwordGenerator;
            this.passwordChangeProvider = passwordChangeProvider;
            this.passwordStorageProvider = passwordStorageProvider;
            this.registrationProvider = registrationProvider;
            this.checkInProvider = checkInProvider;
            this.metadataProvider = metadataProvider;
        }

        public AdvancedLapsAgent(ILogger<AdvancedLapsAgent> logger, ISettingsProvider settings, IPasswordGenerator passwordGenerator, IPasswordChangeProvider passwordChangeProvider, IPasswordStorageProvider passwordStorageProvider, IRegistrationProvider registrationProvider, IAgentCheckInProvider checkInProvider, IMetadataProvider metadataProvider, ILithnetAdminPasswordProvider lithnetAdminPasswordProvider, IDirectory directory, ILocalSam sam, IMsMcsAdmPwdProvider msMcsPasswordProvider)
            :
            this(logger, settings, passwordGenerator, passwordChangeProvider, passwordStorageProvider, registrationProvider, checkInProvider, metadataProvider)
        {
            this.lithnetAdminPasswordProvider = lithnetAdminPasswordProvider;
            this.directory = directory;
            this.sam = sam;
            this.msMcsPasswordProvider = msMcsPasswordProvider;
        }

        public async Task DoCheckAsync()
        {
            var metadata = await this.metadataProvider.GetMetadata();



            if (this.settings.AuthenticationMode == AgentAuthenticationMode.Ssa)
            {
                var state = await this.registrationProvider.GetRegistrationState();

                switch (state)
                {
                    case RegistrationState.NotRegistered:
                        if (this.registrationProvider.CanRegisterAgent())
                        {
                            var result = await this.registrationProvider.RegisterAgent();

                            if (result != RegistrationState.Approved)
                            {
                                return;
                            }
                        }
                        else
                        {
                            this.logger.LogWarning("The client is not able to register. Please ensure the client has an active registration key");
                            return;
                        }

                        break;

                    case RegistrationState.Approved:
                        break;

                    case RegistrationState.Pending:
                    case RegistrationState.Rejected:
                        return;
                }
            }

            await this.checkInProvider.CheckinIfRequired();
            await this.CheckAndChangePassword();
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
                string accountName = this.passwordChangeProvider.GetAccountName();

                await this.passwordStorageProvider.UpdatePassword(accountName, newPassword, expiryDate);

                this.logger.LogTrace(EventIDs.SetPasswordOnAmAttribute, "Password successfully committed to storage");

                try
                {
                    this.PerformCompatibilityStorage(accountName, newPassword, expiryDate);
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

        private void PerformCompatibilityStorage(string accountName, string newPassword, DateTime expiryDate)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            if (this.settings.LithnetLocalAdminPasswordAttributeBehaviour == PasswordAttributeBehaviour.Ignore &&
                this.settings.MsMcsAdmPwdAttributeBehaviour == PasswordAttributeBehaviour.Ignore)
            {
                return;
            }

            if (this.lithnetAdminPasswordProvider == null || this.directory == null || this.sam == null)
            {
                throw new NotSupportedException("One or more of the dependent components was not available");
            }

            if (!this.sam.IsDomainJoined())
            {
                return;
            }

            IActiveDirectoryComputer computer = this.directory.GetComputer(this.sam.GetMachineNTAccountName());

            switch (this.settings.LithnetLocalAdminPasswordAttributeBehaviour)
            {
                case PasswordAttributeBehaviour.Clear:
                    try
                    {
                        this.lithnetAdminPasswordProvider.ClearPassword(computer);
                        this.lithnetAdminPasswordProvider.ClearPasswordHistory(computer);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "Could not clear the lithnet admin password attributes");
                    }

                    break;

                case PasswordAttributeBehaviour.Populate:
                    this.lithnetAdminPasswordProvider.UpdateCurrentPassword(computer, accountName, newPassword, DateTime.UtcNow, expiryDate, this.settings.LithnetLocalAdminPasswordHistoryDaysToKeep, PasswordAttributeBehaviour.Ignore);
                    break;
            }

            switch (this.settings.MsMcsAdmPwdAttributeBehaviour)
            {
                case PasswordAttributeBehaviour.Clear:
                    try
                    {
                        this.msMcsPasswordProvider.ClearPassword(computer);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "Could not clear the ms-Mcs-AdmPwd attributes");
                    }

                    break;

                case PasswordAttributeBehaviour.Populate:
                    this.msMcsPasswordProvider.SetPassword(computer, newPassword, expiryDate);
                    break;
            }
        }
    }
}