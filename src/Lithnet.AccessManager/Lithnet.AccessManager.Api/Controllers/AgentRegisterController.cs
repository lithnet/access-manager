using Lithnet.AccessManager.Api.Providers;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Configuration;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Providers;

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
        private readonly IOptions<AmsManagedDeviceRegistrationOptions> amsManagedDeviceOptions;

        public AgentRegisterController(ILogger<AgentRegisterController> logger, ISignedAssertionValidator assertionValidator, IDeviceProvider devices, IOptions<ApiAuthenticationOptions> agentOptions, IApiErrorResponseProvider errorProvider, ICheckInDataValidator checkInDataValidator, IRegistrationKeyProvider regKeyProvider, IOptions<AmsManagedDeviceRegistrationOptions> amsManagedDeviceOptions)
        {
            this.logger = logger;
            this.assertionValidator = assertionValidator;
            this.devices = devices;
            this.agentOptions = agentOptions;
            this.errorProvider = errorProvider;
            this.checkInDataValidator = checkInDataValidator;
            this.regKeyProvider = regKeyProvider;
            this.amsManagedDeviceOptions = amsManagedDeviceOptions;
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
                string deviceId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                this.logger.LogTrace($"Device {deviceId} is attempting to register a set of secondary credentials");

                IDevice device = await this.devices.GetDeviceAsync(deviceId);

                await this.devices.AddDeviceCredentialsAsync(device, signingCertificate);

                this.logger.LogInformation($"Device {device.ObjectID} from authority {device.AuthorityType}/{device.AuthorityId}/{device.AuthorityDeviceId} has successfully added a secondary credential set with thumbprint {signingCertificate.Thumbprint}");

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

                JwtSecurityToken token = this.assertionValidator.Validate(request.Assertion, "api/v1.0/agent/register", out X509Certificate2 signingCertificate);

                try
                {
                    IDevice existingDevice = await this.devices.GetDeviceAsync(signingCertificate);
                    this.logger.LogInformation($"An agent requested registration, and its certificate {signingCertificate.Thumbprint} was found in the database with device ID {existingDevice.Id}");
                    return this.GetDeviceApprovalResult(existingDevice);
                }
                catch (DeviceCredentialsNotFoundException)
                {
                    this.logger.LogInformation($"A new agent requested registration with certificate {signingCertificate.Thumbprint}");
                }

                IDevice device = await this.ValidateRegistrationClaims(token);
                device = await this.devices.CreateDeviceAsync(device, signingCertificate);

                this.logger.LogInformation($"Created new device {device.ObjectID} associated with the credentials {signingCertificate.Thumbprint}");

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
                this.logger.LogInformation($"The device {device.Id} has been approved");
                return this.Json(new RegistrationResponse { State = "approved", ClientId = device.ObjectID });

            }
            else if (device.ApprovalState == ApprovalState.Pending)
            {
                this.logger.LogInformation($"The device {device.Id} is pending approval");
                JsonResult result = this.Json(new RegistrationResponse { State = "pending", ClientId = device.ObjectID });
                result.StatusCode = StatusCodes.Status202Accepted;
                return result;
            }
            else
            {
                this.logger.LogInformation($"The device {device.Id} has been rejected");
                JsonResult result = this.Json(new RegistrationResponse { State = "rejected", ClientId = device.ObjectID });
                result.StatusCode = StatusCodes.Status410Gone;
                return result;
            }
        }

        private async Task<IDevice> ValidateRegistrationClaims(JwtSecurityToken token)
        {
            string registrationKey = token.Claims.FirstOrDefault(t => t.Type == "registration-key")?.Value;
            if (string.IsNullOrWhiteSpace(registrationKey))
            {
                throw new RegistrationKeyValidationException("The registration information did not include a registration key");
            }

            if (!await this.regKeyProvider.ValidateRegistrationKey(registrationKey))
            {
                throw new RegistrationKeyValidationException("The registration key validation failed");
            }

            string data = token.Claims.FirstOrDefault(t => t.Type == "data")?.Value;
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
                OperatingSystemVersion = checkInData.OperationSystemVersion,
            };

            if (this.amsManagedDeviceOptions.Value.AutoApproveNewDevices)
            {
                device.ApprovalState = ApprovalState.Approved;
            }

            return device;
        }
    }
}