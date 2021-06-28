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

        [HttpPost()]
        public async Task<IActionResult> RegisterAsync([FromBody] ClientAssertion request)
        {
            try
            {
                if (!this.agentOptions.Value.AllowAmsManagedDeviceAuth)
                {
                    this.logger.LogWarning("A client attempted to register, but registration is disabled");
                    return this.Forbid(JwtBearerDefaults.AuthenticationScheme);
                }

                JwtSecurityToken token = this.assertionValidator.Validate(request.Assertion, "api/v1.0/agent/register", out X509Certificate2 signingCertificate);

                Device device = await this.ValidateRegistrationClaims(token);

                device = await this.devices.CreateDeviceAsync(device, signingCertificate);

                return this.GetDeviceApprovalResult(device);
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetRegistrationStatusAsync([FromRoute] string requestId)
        {
            try
            {
                if (!this.agentOptions.Value.AllowAmsManagedDeviceAuth)
                {
                    this.logger.LogWarning("A client attempted to validate its registration status, but registration is disabled");
                    return this.Forbid(JwtBearerDefaults.AuthenticationScheme);
                }

                Device device = await this.devices.GetDeviceAsync(AuthorityType.Ams, Constants.AmsAuthorityId, requestId);

                return this.GetDeviceApprovalResult(device);
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        private IActionResult GetDeviceApprovalResult(Device device)
        {
            if (device.ApprovalState == ApprovalState.Approved)
            {
                return this.Json(new RegistrationResponse { State = "approved", ClientId = device.ObjectID });

            }
            else if (device.ApprovalState == ApprovalState.Pending)
            {
                string newPath = this.Request.PathBase.Add(new PathString($"/agent/register/{device.ObjectID}"));
                this.Response.Headers.Add("Location", newPath);
                JsonResult result = this.Json(new RegistrationResponse { State = "pending", ClientId = device.ObjectID });

                result.StatusCode = StatusCodes.Status202Accepted;
                return result;
            }
            else
            {
                JsonResult result = this.Json(new RegistrationResponse { State = "rejected", ClientId = device.ObjectID });
                result.StatusCode = StatusCodes.Status403Forbidden;
                return result;
            }
        }

        private async Task<Device> ValidateRegistrationClaims(JwtSecurityToken token)
        {
            string registrationKey = token.Claims.FirstOrDefault(t => t.Type == "registration-key")?.Value;
            if (string.IsNullOrWhiteSpace(registrationKey))
            {
                throw new BadRequestException("The registration information did not include a registration key");
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

            Device device = new Device()
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