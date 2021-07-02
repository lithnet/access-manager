using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent
{
    public class AmsLapsAgent
    {
        private readonly ILogger<AmsLapsAgent> logger;
        private readonly IAgentSettings agentSettings;
        private readonly IPasswordGenerator passwordGenerator;
        private readonly IPasswordChangeProvider passwordChangeProvider;
        private readonly IPasswordStorageProvider passwordStorageProvider;
        private readonly IRegistrationProvider registrationProvider;
        private readonly IAgentCheckInProvider checkInProvider;
        private readonly IAadJoinInformationProvider aadJoinInformationProvider;

        public AmsLapsAgent(ILogger<AmsLapsAgent> logger, IPasswordGenerator passwordGenerator, IPasswordChangeProvider passwordChangeProvider, IPasswordStorageProvider passwordStorageProvider, IRegistrationProvider registrationProvider, IAgentCheckInProvider checkInProvider, IAadJoinInformationProvider aadJoinInformationProvider, IAgentSettings agentSettings)
        {
            this.logger = logger;
            this.passwordGenerator = passwordGenerator;
            this.passwordChangeProvider = passwordChangeProvider;
            this.passwordStorageProvider = passwordStorageProvider;
            this.registrationProvider = registrationProvider;
            this.checkInProvider = checkInProvider;
            this.aadJoinInformationProvider = aadJoinInformationProvider;
            this.agentSettings = agentSettings;
        }

        public async Task DoCheckAsync()
        {
            try
            {
                if (await CanContinue())
                {
                    await this.checkInProvider.CheckinIfRequired();
                    await this.CheckAndChangePassword();
                }
            }
            catch (ApiException ex)
            {
                if (!this.TryHandleException(ex))
                {
                    throw;
                }

            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException s)
            {
                this.logger.LogError($"Unable to connect to the server {this.agentSettings.Server} due to error {s.SocketErrorCode}: {s.Message}");
                this.logger.LogTrace(ex, "Unable to connect to server");
            }
            catch (HttpRequestException ex)
            {
                this.logger.LogError($"Unable to connect to the server {this.agentSettings.Server}. {ex.Message}");
                this.logger.LogTrace(ex, "Unable to connect to server");
            }
            catch (SocketException s)
            {
                this.logger.LogError($"Unable to connect to the server {this.agentSettings.Server} due to error {s.SocketErrorCode}: {s.Message}");
                this.logger.LogTrace(s, "Unable to connect to server");
            }
        }

        private bool TryHandleException(ApiException ex)
        {
            if (ex.ApiErrorCode == ApiConstants.DeviceCredentialsNotFound)
            {
                if (this.agentSettings.AuthenticationMode == AgentAuthenticationMode.Aad)
                {
                    this.agentSettings.HasRegisteredSecondaryCredentials = false;
                    this.logger.LogError("The server indicated that it no longer recognizes this agent. The agent will attempt to re-set up the relationship with the server on the next run");
                }
                else if (this.agentSettings.AuthenticationMode == AgentAuthenticationMode.Ams)
                {
                    if (this.agentSettings.RegistrationState == RegistrationState.Approved && !string.IsNullOrWhiteSpace(this.agentSettings.RegistrationKey))
                    {
                        this.logger.LogError("The server indicated that it no longer recognizes this agent. The agent will attempt to re-register the device with the current registration key on the next run");
                        this.agentSettings.RegistrationState = RegistrationState.NotRegistered;
                    }
                }
            }

            return false;
        }

        private async Task<bool> CanContinue()
        {
            if (string.IsNullOrWhiteSpace(this.agentSettings.Server))
            {
                this.logger.LogError("No AMS server was configured");
                return false;
            }

            if (this.agentSettings.AuthenticationMode == AgentAuthenticationMode.Ams)
            {
                return await this.CanContinueAms();
            }

            if (this.agentSettings.AuthenticationMode == AgentAuthenticationMode.Aad)
            {
                return await this.CanContinueAad();
            }

            this.logger.LogTrace("Cannot continue because an unsupported auth mode is configured");

            return false;
        }

        private async Task<bool> CanContinueAad()
        {
            if (this.agentSettings.HasRegisteredSecondaryCredentials)
            {
                this.logger.LogTrace("Device has registered secondary credentials");
                return true;
            }

            if (!this.aadJoinInformationProvider.InitializeJoinInformation())
            {
                this.logger.LogTrace("AAD join information was not found");
                return false;
            }

            if (this.aadJoinInformationProvider.IsDeviceJoined && !this.agentSettings.RegisterSecondaryCredentialsForAadj)
            {
                this.logger.LogTrace("Device is AAD joined and secondary credentials are not required");
                return true;
            }

            if (this.aadJoinInformationProvider.IsDeviceJoined && this.agentSettings.RegisterSecondaryCredentialsForAadj)
            {
                this.logger.LogTrace("Device is AAD joined and secondary credentials are required, but not yet registered");
                await this.registrationProvider.RegisterSecondaryCredentials();
                return true;
            }

            if (!this.agentSettings.RegisterSecondaryCredentialsForAadr)
            {
                this.logger.LogWarning("Cannot perform AAD authentication because the device is not AAD joined, and the current agent settings do not permit registering AADR credentials");
                return false;
            }

            if (this.aadJoinInformationProvider.IsWorkplaceJoined)
            {
                if (!this.agentSettings.HasRegisteredSecondaryCredentials)
                {
                    await this.registrationProvider.RegisterSecondaryCredentials();
                    return true;
                }
            }

            this.logger.LogTrace("Cannot continue because AAD state is unknown");
            return false;
        }

        protected virtual async Task<bool> CanContinueAms()
        {
            this.logger.LogTrace("Checking registration state for AMS authentication");
            var state = await this.registrationProvider.GetRegistrationState();
            this.logger.LogTrace($"Check registration state returned {state}");

            switch (state)
            {
                case RegistrationState.NotRegistered:
                    if (this.registrationProvider.CanRegisterAgent())
                    {
                        var result = await this.registrationProvider.RegisterAgent();

                        if (result != RegistrationState.Approved)
                        {
                            this.logger.LogWarning("The client has registered and is pending approval. Registration state will be checked on the next agent cycle");
                            return false;
                        }
                    }
                    else
                    {
                        this.logger.LogWarning("The client is not able to register. Please ensure the client has an active registration key");
                        return false;
                    }

                    break;

                case RegistrationState.Approved:
                    break;

                case RegistrationState.Pending:
                case RegistrationState.Rejected:
                    this.logger.LogTrace($"Cannot continue because AMS state is {state}");
                    return false;
            }

            return true;
        }

        private async Task CheckAndChangePassword()
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

        private async Task ChangePassword()
        {
            try
            {
                var policy = this.passwordStorageProvider.GetPolicy();

                string newPassword = this.passwordGenerator.Generate(policy);
                DateTime expiryDate = DateTime.UtcNow.AddDays(Math.Max(policy.MaximumPasswordAgeDays, 1));
                string accountName = this.passwordChangeProvider.GetAccountName();

                await this.passwordStorageProvider.UpdatePassword(accountName, newPassword, expiryDate);

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