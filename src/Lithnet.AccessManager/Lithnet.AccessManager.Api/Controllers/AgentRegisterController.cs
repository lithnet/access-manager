using Lithnet.AccessManager.Api.Providers;
using Lithnet.AccessManager.Api.Shared;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("agent/register")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class AgentRegisterController : Controller
    {
        private readonly ILogger<AgentRegisterController> logger;
        private readonly ISignedAssertionValidator assertionValidator;
        private readonly IDeviceProvider devices;
        private readonly IOptions<ApiAuthenticationOptions> agentOptions;
        private readonly IApiErrorResponseProvider errorProvider;
        private readonly ICheckInDataValidator checkInDataValidator;
        private readonly IRegistrationKeyProvider regKeyProvider;
        private readonly IAmsLicenseManager licenseManager;
        private readonly IAmsGroupProvider groupProvider;

        public AgentRegisterController(ILogger<AgentRegisterController> logger, ISignedAssertionValidator assertionValidator, IDeviceProvider devices, IOptions<ApiAuthenticationOptions> agentOptions, IApiErrorResponseProvider errorProvider, ICheckInDataValidator checkInDataValidator, IRegistrationKeyProvider regKeyProvider, IAmsLicenseManager licenseManager, IAmsGroupProvider groupProvider)
        {
            this.logger = logger;
            this.assertionValidator = assertionValidator;
            this.devices = devices;
            this.agentOptions = agentOptions;
            this.errorProvider = errorProvider;
            this.checkInDataValidator = checkInDataValidator;
            this.regKeyProvider = regKeyProvider;
            this.licenseManager = licenseManager;
            this.groupProvider = groupProvider;
        }

        [HttpPost("credential")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Constants.AuthZPolicyComputers)]
        [Authorize(Constants.AuthZPolicyApprovedClient)]
        [Authorize(Constants.AuthZPolicyAuthorityAzureAd)]
        public async Task<IActionResult> RegisterAdditionalCredentials([FromBody] ClientAssertion request)
        {
            try
            {
                JwtSecurityToken token = this.assertionValidator.Validate(request.Assertion, "api/v1.0/agent/register/credential", out X509Certificate2 signingCertificate);
                string deviceId = this.HttpContext.GetDeviceIdOrThrow();

                this.logger.LogTrace("Device {deviceId} is attempting to register a set of secondary credentials", deviceId);

                IDevice device = await this.devices.GetDeviceAsync(deviceId);
                device.ThrowOnDeviceDisabled();

                try
                {
                    IDevice existingDevice = await this.devices.GetDeviceAsync(signingCertificate);

                    if (existingDevice.ObjectID == device.ObjectID)
                    {
                        this.logger.LogInformation("Device {deviceId} requested to add additional credentials with thumbprint {thumbprint} but they were already known to the server", deviceId, signingCertificate.Thumbprint);
                        return this.Ok();
                    }
                    else
                    {
                        this.logger.LogError("Device {deviceId} requested to add additional credentials with thumbprint {thumbprint} but they were already known to the server for a different device {existingDeviceId}", deviceId, signingCertificate.Thumbprint, existingDevice.Id);
                        return this.Conflict();
                    }
                }
                catch (DeviceCredentialsNotFoundException)
                {
                }

                await this.devices.AddDeviceCredentialsAsync(device, signingCertificate);

                this.logger.LogInformation("Device {deviceId} from authority {authorityType}/{authorityId}/{authorityDeviceId} has successfully added a secondary credential set with thumbprint {thumbprint}", device.ObjectID, device.AuthorityType, device.AuthorityId, device.AuthorityDeviceId, signingCertificate.Thumbprint);

                return this.Ok();
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        [HttpPost()]
        public async Task<IActionResult> RegisterAsync([FromBody] ClientAssertion request)
        {
            try
            {
                if (!this.agentOptions.Value.AllowAmsManagedDeviceAuth)
                {
                    throw new RegistrationDisabledException("A client attempted to register, but registration is disabled");
                }

                this.licenseManager.ThrowOnMissingFeature(LicensedFeatures.AmsRegisteredDeviceSupport);

                JwtSecurityToken token = this.assertionValidator.Validate(request.Assertion, "api/v1.0/agent/register", out X509Certificate2 signingCertificate);

                try
                {
                    IDevice existingDevice = await this.devices.GetDeviceAsync(signingCertificate);
                    existingDevice.ThrowOnDeviceDisabled();
                    this.logger.LogInformation("An agent requested registration, and its certificate {thumbprint} was found in the database with device ID {deviceId}", signingCertificate.Thumbprint, existingDevice.ObjectID);

                    return this.GetDeviceApprovalResult(existingDevice);
                }
                catch (DeviceCredentialsNotFoundException)
                {
                    this.logger.LogInformation("A new agent requested registration with certificate {thumbprint} from IP {ip}", signingCertificate.Thumbprint, this.HttpContext.Connection.RemoteIpAddress);
                }

                IRegistrationKey registrationKey = await this.ValidateRegistrationClaims(token);
                var device = this.ConstructDeviceFromCheckInDetails(token, registrationKey);
                device = await this.devices.CreateDeviceAsync(device, signingCertificate);
                await this.ProcessRegistrationKeyGroups(registrationKey, device);

                this.logger.LogInformation("Created new device {deviceId} associated with the credentials {thumbprint}", device.ObjectID, signingCertificate.Thumbprint);

                return this.GetDeviceApprovalResult(device);
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        private IActionResult GetDeviceApprovalResult(IDevice device)
        {
            if (device.ApprovalState == ApprovalState.Approved)
            {
                this.logger.LogInformation("The device {deviceId} has been approved", device.ObjectID);
                return this.Ok(new RegistrationResponse { ApprovalState = ApprovalState.Approved, ClientId = device.ObjectID });

            }
            else if (device.ApprovalState == ApprovalState.Pending)
            {
                this.logger.LogInformation("The device {deviceId} is pending approval", device.ObjectID);
                return this.Ok(new RegistrationResponse { ApprovalState = ApprovalState.Pending, ClientId = device.ObjectID });
            }
            else
            {
                this.logger.LogInformation("The device {deviceId} has been rejected", device.ObjectID);
                return this.Ok(new RegistrationResponse { ApprovalState = ApprovalState.Rejected, ClientId = device.ObjectID });
            }
        }

        private async Task<IRegistrationKey> ValidateRegistrationClaims(JwtSecurityToken token)
        {
            string key = token.Claims.FirstOrDefault(t => t.Type == AmsClaimNames.RegistrationKey)?.Value;
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new RegistrationKeyValidationException("The registration information did not include a registration key");
            }

            var registrationKey = await this.regKeyProvider.ValidateRegistrationKey(key);

            if (registrationKey == null)
            {
                throw new RegistrationKeyValidationException("The registration key validation failed");
            }

            return registrationKey;
        }

        private IDevice ConstructDeviceFromCheckInDetails(JwtSecurityToken token, IRegistrationKey registrationKey)
        {
            string data = token.Claims.FirstOrDefault(t => t.Type == AmsClaimNames.Data)?.Value;
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new BadRequestException("The registration information did not include a data element");
            }

            AgentCheckIn checkInData = JsonSerializer.Deserialize<AgentCheckIn>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new BadRequestException("The registration data element could not be decoded");

            this.checkInDataValidator.ValidateCheckInData(checkInData);

            IDevice device = new DbDevice()
            {
                ComputerName = checkInData.Hostname,
                DnsName = checkInData.DnsName,
                AgentVersion = checkInData.AgentVersion,
                OperatingSystemFamily = checkInData.OperatingSystem,
                OperatingSystemVersion = checkInData.OperatingSystemVersion,
                OperatingSystemType = checkInData.OperatingSystemType
            };

            device.ApprovalState = registrationKey.ApprovalRequired ? ApprovalState.Pending : ApprovalState.Approved;

            this.logger.LogInformation("Validated registration key '{registrationKeyName}' provided by device '{deviceName}'. Device approval state is '{approvalState}'", registrationKey.Name, device.Name, device.ApprovalState);

            return device;
        }

        private async Task ProcessRegistrationKeyGroups(IRegistrationKey key, IDevice device)
        {
            try
            {
                await foreach (var group in this.regKeyProvider.GetRegistrationKeyGroups(key))
                {
                    try
                    {
                        await this.groupProvider.AddToGroup(group, device);
                        this.logger.LogInformation("Add device '{deviceName}' to group '{groupName}'", device.Name, group.Name);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "Failed to add device '{deviceName}' to group '{groupName}' associated with registration key '{registrationKeyName}'", device.Name, group.Name, key.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to get list of groups associated with the registration key from the database");
            }
        }
    }
}