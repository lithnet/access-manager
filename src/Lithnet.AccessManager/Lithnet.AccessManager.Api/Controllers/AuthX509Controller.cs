using Lithnet.AccessManager.Api.Providers;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("auth/x509")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ResponseCache(NoStore = true, Duration = 0)]
    public class AuthX509Controller : Controller
    {
        private readonly ILogger<AuthX509Controller> logger;
        private readonly ISecurityTokenGenerator tokenGenerator;
        private readonly IDeviceProvider devices;
        private readonly ISignedAssertionValidator signedAssertionValidator;
        private readonly IAadGraphApiProvider graphProvider;
        private readonly IOptions<AgentOptions> agentOptions;
        private readonly IOptions<AzureAdOptions> azureAdOptions;
        private readonly IApiErrorResponseProvider errorProvider;

        public AuthX509Controller(ISecurityTokenGenerator tokenGenerator, IDeviceProvider devices, ISignedAssertionValidator signedAssertionValidator, IAadGraphApiProvider graphProvider, ILogger<AuthX509Controller> logger, IOptions<AgentOptions> agentOptions, IOptions<AzureAdOptions> azureAdOptions, IApiErrorResponseProvider errorProvider)
        {
            this.tokenGenerator = tokenGenerator;
            this.devices = devices;
            this.signedAssertionValidator = signedAssertionValidator;
            this.graphProvider = graphProvider;
            this.logger = logger;
            this.agentOptions = agentOptions;
            this.azureAdOptions = azureAdOptions;
            this.errorProvider = errorProvider;
        }

        [HttpPost]
        public async Task<IActionResult> ValidateAssertionAsync([FromBody] ClientAssertion request)
        {
            try
            {
                if (!this.agentOptions.Value.AllowAadAuth && !this.agentOptions.Value.AllowSelfSignedAuth)
                {
                    this.logger.LogWarning("A client attempted to authenticate with a signed assertion, but no assertion-enabled authentication methods are enabled");
                    throw new UnsupportedAuthenticationTypeException();
                }

                this.signedAssertionValidator.Validate(request.Assertion, "auth/x509", out X509Certificate2 signingCertificate);

                TokenResponse token;

                if (this.IsAadCertificate(signingCertificate))
                {
                    if (this.agentOptions.Value.AllowAadAuth)
                    {
                        token = await this.ValidateAadAssertionAsync(signingCertificate);
                    }
                    else
                    {
                        throw new UnsupportedAuthenticationTypeException($"The device presented an Azure Active Directory certificate ({signingCertificate.Subject}), but AAD authentication is not enabled");
                    }
                }
                else
                {
                    if (this.agentOptions.Value.AllowSelfSignedAuth)
                    {
                        token = await this.ValidateSelfSignedAssertionAsync(signingCertificate);
                    }
                    else
                    {
                        throw new UnsupportedAuthenticationTypeException($"The device presented an self-signed assertion, but self-asserted device authentication is not enabled");
                    }
                }

                return this.Ok(token);
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
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

        private async Task<TokenResponse> ValidateAadAssertionAsync(X509Certificate2 signingCertificate)
        {
            if (signingCertificate.Subject == null || !signingCertificate.Subject.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("The certificate subject was invalid");
            }

            string deviceId = signingCertificate.Subject.Remove(0, 3);

            this.logger.LogTrace($"Client has presented an Azure AD certificate for authentication of device {deviceId}");

            Microsoft.Graph.Device aadDevice = await this.graphProvider.GetAadDeviceByDeviceIdAsync(deviceId);

            if (!aadDevice.HasDeviceThumbprint(signingCertificate.Thumbprint))
            {
                throw new AadDeviceNotFoundException($"The certificate thumbprint '{signingCertificate.Thumbprint}' could not be found on device {deviceId} in the Azure Active Directory");
            }

            aadDevice.ThrowOnDeviceDisabled();

            switch (aadDevice.TrustType.ToLowerInvariant())
            {
                case "azuread":
                    if (!this.agentOptions.Value.AllowAzureAdJoinedDevices)
                    {
                        throw new UnsupportedAuthenticationTypeException("The device is Azure AD joined, but Azure AD-joined devices are not permitted to authenticate to the system");
                    }
                    break;

                case "workplace":
                    if (!this.agentOptions.Value.AllowAzureAdRegisteredDevices)
                    {
                        throw new UnsupportedAuthenticationTypeException("The device is Azure AD registered, but Azure AD-registered devices are not permitted to authenticate to the system");
                    }
                    break;

                default:
                    throw new UnsupportedAuthenticationTypeException($"The AAD device has an unknown trust type '{aadDevice.TrustType}'");
            }

            Device device = await this.devices.GetOrCreateDeviceAsync(aadDevice, this.azureAdOptions.Value.TenantId);
            ClaimsIdentity identity = device.ToClaimsIdentity();

            this.logger.LogInformation($"Successfully authenticated device {device.ComputerName} ({device.ObjectID}) from AzureAD");
            return this.tokenGenerator.GenerateToken(identity);
        }

        private async Task<TokenResponse> ValidateSelfSignedAssertionAsync(X509Certificate2 signingCertificate)
        {
            Device device = await this.devices.GetDeviceAsync(signingCertificate);
            ClaimsIdentity identity = device.ToClaimsIdentity();

            this.logger.LogInformation($"Successfully authenticated device {device.ComputerName} ({device.ObjectID}) using a self-signed assertion");
            return this.tokenGenerator.GenerateToken(identity);
        }
    }
}
