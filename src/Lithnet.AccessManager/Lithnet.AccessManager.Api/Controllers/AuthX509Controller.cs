using Lithnet.AccessManager.Api.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("auth/x509")]
    [AllowAnonymous]
    public class AuthX509Controller : Controller
    {
        private readonly ILogger<AuthX509Controller> logger;
        private readonly ISecurityTokenGenerator tokenGenerator;
        private readonly IDeviceProvider devices;
        private readonly ISelfSignedAssertionValidator assertionValidator;
        private readonly IReplayNonceProvider nonceProvider;
        private readonly IConfiguration config;
        private readonly IAadGraphApiProvider graphProvider;
        private readonly IOptions<AzureAdOptions> azureAdOptions;

        public AuthX509Controller(ISecurityTokenGenerator tokenGenerator, IDeviceProvider devices, ISelfSignedAssertionValidator assertionValidator, IReplayNonceProvider nonceProvider, IConfiguration config, IAadGraphApiProvider graphProvider, ILogger<AuthX509Controller> logger, IOptions<AzureAdOptions> azureAdOptions)
        {
            this.tokenGenerator = tokenGenerator;
            this.devices = devices;
            this.assertionValidator = assertionValidator;
            this.nonceProvider = nonceProvider;
            this.config = config;
            this.graphProvider = graphProvider;
            this.logger = logger;
            this.azureAdOptions = azureAdOptions;
        }

        [HttpPost]
        public async Task<IActionResult> ValidateAssertionAsync([FromBody] RegistrationRequest request)
        {
            try
            {
                this.assertionValidator.Validate(request.Assertion, "https://localhost:44385/api/v1.0/agent/register", out X509Certificate2 signingCertificate);

                string token;

                if (this.IsAadCertificate(signingCertificate))
                {
                    if (this.azureAdOptions.Value.AllowAzureAdJoinedDevices || this.azureAdOptions.Value.AllowAzureAdRegisteredDevices)
                    {
                        token = await this.ValidateAadAssertionAsync(signingCertificate);
                    }
                    else
                    {
                        this.logger.LogError($"The device presented an Azure Active Directory certificate ({signingCertificate.Subject}), but AAD authentication is not enabled");
                        return this.Unauthorized();
                    }
                }
                else
                {
                    token = await this.ValidateSelfSignedAssertionAsync(signingCertificate);
                }

                return this.Json(new { access_token = token });
            }
            catch (BadRequestException ex)
            {
                this.logger.LogError(ex, "The request could not be processed due to an input error");
                return this.BadRequest();
            }
            catch (ObjectNotFoundException ex)
            {
                this.logger.LogError(ex, "The device could not be found");
                return this.Unauthorized();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The request could not be processed");
                throw;
            }
        }

        [HttpGet]
        public IActionResult GetNonce()
        {
            this.Response.Headers.Add("Replay-Nonce", this.nonceProvider.GenerateNonce());
            return this.Ok();
        }

        private bool IsAadCertificate(X509Certificate2 signingCertificate)
        {
            X500DistinguishedName dn1 = new X500DistinguishedName(signingCertificate.IssuerName.Format(false).Replace('+', ','));

            foreach (string dn in this.azureAdOptions.Value.AadIssuerDNs)
            {
                if (dn1.IsDnMatch(dn))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<string> ValidateAadAssertionAsync(X509Certificate2 signingCertificate)
        {
            if (signingCertificate.Subject == null || !signingCertificate.Subject.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("The certificate subject was invalid");
            }

            string deviceId = signingCertificate.Subject.Remove(0, 3);

            this.logger.LogTrace($"Client has presented an AzureAD join certificate for authentication of device {deviceId}");

            Microsoft.Graph.Device aadDevice = await this.graphProvider.GetAadDeviceAsync(deviceId);

            if (!aadDevice.HasDeviceThumbprint(signingCertificate.Thumbprint))
            {
                throw new CertificateIdentityNotFoundException($"The certificate thumbprint '{signingCertificate.Thumbprint}' could not be found on device {deviceId} in the Azure Active Directory");
            }

            aadDevice.ThrowOnDeviceDisabled();
            //aadDevice.TrustType == "AzureAD"

            Device device = await this.devices.GetOrCreateDeviceAsync(aadDevice, this.config["TenantID"]);
            ClaimsIdentity identity = device.ToClaimsIdentity();

            this.logger.LogInformation($"Successfully authenticated device {device.ComputerName} ({device.ObjectId}) from AzureAD");
            return this.tokenGenerator.GenerateToken(identity);
        }

        private async Task<string> ValidateSelfSignedAssertionAsync(X509Certificate2 signingCertificate)
        {
            Device device = await this.devices.GetDeviceAsync(signingCertificate);
            ClaimsIdentity identity = device.ToClaimsIdentity();

            this.logger.LogInformation($"Successfully authenticated device {device.ComputerName} ({device.ObjectId}) using a self-signed assertion");
            return this.tokenGenerator.GenerateToken(identity);
        }
    }
}
