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
using Microsoft.AspNetCore.Http;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("agent/register")]
    [AllowAnonymous]
    public class AgentRegisterController : Controller
    {
        private readonly ILogger<AgentRegisterController> logger;
        private readonly IReplayNonceProvider nonceProvider;
        private readonly ICertificateProvider certificateProvider;
        private readonly ISelfSignedAssertionValidator assertionValidator;
        private readonly IDeviceProvider devices;

        public AgentRegisterController(ILogger<AgentRegisterController> logger, IReplayNonceProvider nonceProvider, ICertificateProvider certificateProvider, ISelfSignedAssertionValidator assertionValidator, IDeviceProvider devices)
        {
            this.logger = logger;
            this.nonceProvider = nonceProvider;
            this.certificateProvider = certificateProvider;
            this.assertionValidator = assertionValidator;
            this.devices = devices;
        }

        [HttpPost()]
        public async Task<IActionResult> RegisterAsync([FromBody] RegistrationRequest request)
        {
            try
            {
                JwtSecurityToken token = this.assertionValidator.Validate(request.Assertion, "https://localhost:44385/api/v1.0/agent/register", out X509Certificate2 signingCertificate);

                Device device = this.ValidateRegistrationClaims(token);

                device = await this.devices.CreateDeviceAsync(device, signingCertificate);

                return this.GetDeviceApprovalResult(device);
            }
            catch (BadRequestException ex)
            {
                this.logger.LogError(ex, "The request could not be processed due to an input error");
                return this.BadRequest();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The request could not be processed");
                throw;
            }
        }

        [HttpGet("{requestId}")]
        public async Task<IActionResult> GetStatus([FromRoute] string requestId)
        {
            Device device = await this.devices.GetDeviceAsync(AuthorityType.SelfAsserted, "ams", requestId);

            return this.GetDeviceApprovalResult(device);
        }

        private IActionResult GetDeviceApprovalResult(Device device)
        {
            if (device.ApprovalState == ApprovalState.Approved)
            {
                return this.Json(new { state = "approved", client_id = device.Id });
            }
            else if (device.ApprovalState == ApprovalState.Pending)
            {
                string newPath = this.Request.PathBase + this.Request.Path.Add(new PathString($"/{device.ObjectId}"));
                this.Response.Headers.Add("Location", newPath);
                JsonResult result = this.Json(new { state = "pending" });
                result.StatusCode = StatusCodes.Status202Accepted;
                return result;
            }
            else
            {
                JsonResult result = this.Json(new { state = "rejected" });
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

            string dnsName = token.Claims.FirstOrDefault(t => t.Type == "dns-name")?.Value;
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
            string dnsName = "carbon.lithnet.local";
            string registrationKey = "1234";

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
                    new Claim("hostname", hostname),
                    new Claim("dns-name", dnsName),
                    new Claim("registration-key", registrationKey),
                    new Claim("nonce", this.nonceProvider.GenerateNonce()),
                }),
                Expires = DateTime.UtcNow.AddHours(1),
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
                    new Claim("dns-name", dnsName),
                    new Claim("registration-key", registrationKey),
                    new Claim("nonce", this.nonceProvider.GenerateNonce()),
                }),
                Expires = DateTime.UtcNow.AddHours(1),
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
