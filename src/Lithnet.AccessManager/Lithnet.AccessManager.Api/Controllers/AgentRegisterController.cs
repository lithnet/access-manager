using Lithnet.AccessManager.Api.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("agent/register")]
    [Produces("application/json")]
    [AllowAnonymous]
    public class AgentRegisterController : Controller
    {
        private readonly ILogger<AgentRegisterController> logger;
        private readonly ICertificateProvider certificateProvider;
        private readonly ISignedAssertionValidator assertionValidator;
        private readonly IDeviceProvider devices;
        private readonly IOptions<AgentOptions> agentOptions;
        private readonly IApiErrorResponseProvider errorProvider;
        private readonly RandomStringGenerator randomStringGenerator;

        public AgentRegisterController(ILogger<AgentRegisterController> logger, ICertificateProvider certificateProvider, ISignedAssertionValidator assertionValidator, IDeviceProvider devices, IOptions<AgentOptions> agentOptions, IApiErrorResponseProvider errorProvider)
        {
            this.logger = logger;
            this.certificateProvider = certificateProvider;
            this.assertionValidator = assertionValidator;
            this.devices = devices;
            this.agentOptions = agentOptions;
            this.errorProvider = errorProvider;
        }

        [HttpPost()]
        public async Task<IActionResult> RegisterAsync([FromBody] ClientAssertion request)
        {
            try
            {
                if (!this.agentOptions.Value.AllowSelfSignedAuth)
                {
                    this.logger.LogWarning("A client attempted to register, but registration is disabled");
                    return this.Forbid(JwtBearerDefaults.AuthenticationScheme);
                }

                JwtSecurityToken token = this.assertionValidator.Validate(request.Assertion, "https://localhost:44385/api/v1.0/agent/register", out X509Certificate2 signingCertificate);

                Device device = this.ValidateRegistrationClaims(token);
                device.ApprovalState = this.agentOptions.Value.AutoApproveSelfSignedAuth ? ApprovalState.Approved : ApprovalState.Pending;

                device = await this.devices.CreateDeviceAsync(device, signingCertificate);

                return this.GetDeviceApprovalResult(device);
            }

            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetStatus([FromRoute] string requestId)
        {
            try
            {
                if (!this.agentOptions.Value.AllowSelfSignedAuth)
                {
                    this.logger.LogWarning("A client attempted to validate its registration status, but registration is disabled");
                    return this.Forbid(JwtBearerDefaults.AuthenticationScheme);
                }

                Device device = await this.devices.GetDeviceAsync(AuthorityType.SelfAsserted, "ams", requestId);

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
                return this.Json(new RegistrationResponse { State = "approved", ClientId = device.ObjectId });

            }
            else if (device.ApprovalState == ApprovalState.Pending)
            {
                string newPath = this.Request.PathBase + this.Request.Path.Add(new PathString($"/{device.ObjectId}"));
                this.Response.Headers.Add("Location", newPath);
                JsonResult result = this.Json(new RegistrationResponse { State = "pending", ClientId = device.ObjectId });

                result.StatusCode = StatusCodes.Status202Accepted;
                return result;
            }
            else
            {
                JsonResult result = this.Json(new RegistrationResponse { State = "rejected", ClientId = device.ObjectId });
                result.StatusCode = StatusCodes.Status403Forbidden;
                return result;
            }
        }

        private Device ValidateRegistrationClaims(JwtSecurityToken token)
        {
            string computerName = token.Claims.FirstOrDefault(t => t.Type == "hostname")?.Value;
            if (computerName == null)
            {
                throw new BadRequestException("The registration information did not include a hostname");
            }

            string dnsName = token.Claims.FirstOrDefault(t => t.Type == "dnsname")?.Value;
            if (dnsName == null)
            {
                throw new BadRequestException("The registration information did not include a dns name");
            }

            string registrationKey = token.Claims.FirstOrDefault(t => t.Type == "registration-key")?.Value;
            if (registrationKey == null)
            {
                throw new BadRequestException("The registration information did not include a registration key");
            }

            Device device = new Device()
            {
                ComputerName = computerName,
                DnsName = dnsName,
            };

            return device;
        }

        [HttpGet("aad")]
        public IActionResult GetAadJwt()
        {
            string hostname = "carbon";
            //string dnsName = "carbon.lithnet.local";
            //string registrationKey = "1234";

            //CN = d763852d-0c7b-4dce-8532-d3a21ead0140
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2 cert = store.Certificates.Find(X509FindType.FindBySubjectName, "d763852d-0c7b-4dce-8532-d3a21ead0140", false)[0];
            RsaSecurityKey rsaSecurityKey = new RsaSecurityKey(cert.GetRSAPrivateKey());

            string exportedCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));

            string myIssuer = hostname;
            string myAudience = "https://localhost:44385/api/v1.0/agent/register";

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("jti", Guid.NewGuid().ToString()),
                }),
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(4), 
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            };

            // Add x5c header parameter containing the signing certificate:
            JwtSecurityToken token = tokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;
            token.Header.Add(JwtHeaderParameterNames.X5c, new List<string> { exportedCertificate });

            string t = tokenHandler.WriteToken(token);

            return this.Ok($"{t}");
        }

        [HttpGet("{hostname}/{dnsName}/{registrationKey}")]
        public IActionResult GetJwt([FromRoute] string hostname, [FromRoute] string dnsName, [FromRoute] string registrationKey)
        {
            X509Certificate2 cert = this.certificateProvider.CreateSelfSignedCert(Environment.MachineName, new Oid("1.2.3.4.5.6.7.8"));
            RsaSecurityKey rsaSecurityKey = new RsaSecurityKey(cert.GetRSAPrivateKey());

            string exportedCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));

            string myIssuer = hostname;
            string myAudience = "https://localhost:44385/api/v1.0/agent/register";

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("jti", Guid.NewGuid().ToString()),
                    new Claim("hostname", hostname),
                    new Claim("dnsname", dnsName),
                    new Claim("registration-key", registrationKey),
                }),
                Expires = DateTime.UtcNow.AddMinutes(4),
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            };

            // Add x5c header parameter containing the signing certificate:
            JwtSecurityToken token = tokenHandler.CreateToken(tokenDescriptor) as JwtSecurityToken;
            token.Header.Add(JwtHeaderParameterNames.X5c, new List<string> { exportedCertificate });

            string t = tokenHandler.WriteToken(token);

            return this.Ok($"{t}");
        }
    }
}
