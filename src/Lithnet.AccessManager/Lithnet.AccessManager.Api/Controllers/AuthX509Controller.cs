using Lithnet.AccessManager.Api.Providers;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using Microsoft.IdentityModel.Tokens;

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
        private readonly IOptionsMonitor<ApiAuthenticationOptions> agentOptions;
        private readonly IApiErrorResponseProvider errorProvider;

        public AuthX509Controller(ISecurityTokenGenerator tokenGenerator, IDeviceProvider devices, ISignedAssertionValidator signedAssertionValidator, IAadGraphApiProvider graphProvider, ILogger<AuthX509Controller> logger, IOptionsMonitor<ApiAuthenticationOptions> agentOptions, IApiErrorResponseProvider errorProvider)
        {
            this.tokenGenerator = tokenGenerator;
            this.devices = devices;
            this.signedAssertionValidator = signedAssertionValidator;
            this.graphProvider = graphProvider;
            this.logger = logger;
            this.agentOptions = agentOptions;
            this.errorProvider = errorProvider;
        }

        [HttpPost]
        public async Task<IActionResult> ValidateAssertionAsync([FromBody] ClientAssertion request)
        {
            try
            {
                var options = this.agentOptions.CurrentValue;

                if (!options.AllowAadAuth && !options.AllowAmsManagedDeviceAuth)
                {
                    this.logger.LogWarning("A client attempted to authenticate with a signed assertion, but no assertion-enabled authentication methods are enabled");
                    throw new UnsupportedAuthenticationTypeException();
                }

                var unvalidatedToken = this.signedAssertionValidator.Validate(request.Assertion, "api/v1.0/auth/x509", out X509Certificate2 signingCertificate);

                var authModeClaim = this.GetClaimOrThrowIfMissingOrNull(unvalidatedToken.Claims, "auth-mode");

                if (!Enum.TryParse(authModeClaim, out AgentAuthenticationMode authMode))
                {
                    throw new SecurityTokenValidationException($"The value provided for the 'auth-mode' claim was not valid '{authModeClaim}'");
                }

                TokenResponse token;

                if (authMode == AgentAuthenticationMode.Aadj)
                {
                    if (options.AllowAadAuth)
                    {
                        var tenantId = this.GetClaimOrThrowIfMissingOrNull(unvalidatedToken.Claims, "aad-tenant-id");
                        var deviceId = this.GetClaimOrThrowIfMissingOrNull(unvalidatedToken.Claims, "aad-device-id");

                        token = await this.ValidateAadAssertionAsync(signingCertificate, tenantId, deviceId);
                    }
                    else
                    {
                        throw new UnsupportedAuthenticationTypeException($"The device presented an Azure Active Directory certificate ({signingCertificate.Subject}), but AAD authentication is not enabled");
                    }
                }
                else if (authMode == AgentAuthenticationMode.Ssa)
                {
                    if (options.AllowAmsManagedDeviceAuth)
                    {
                        token = await this.ValidateSelfSignedAssertionAsync(signingCertificate);
                    }
                    else
                    {
                        throw new UnsupportedAuthenticationTypeException($"The device presented an self-signed assertion, but self-asserted device authentication is not enabled");
                    }
                }
                else
                {
                    throw new SecurityTokenValidationException($"The value provided for the 'auth-mode' claim was not supported '{authModeClaim}'");
                }

                return this.Ok(token);
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        private string GetClaimOrThrowIfMissingOrNull(IEnumerable<Claim> claims, string requiredClaim)
        {
            var claim = claims.FirstOrDefault(t => t.Type ==requiredClaim) ?? throw new SecurityTokenValidationException($"The token did not contain the expected {requiredClaim} claim");

            if (string.IsNullOrWhiteSpace(claim.Value))
            {
                throw new SecurityTokenValidationException($"The token contained an empty {requiredClaim} claim");
            }

            return claim.Value;
        }

        //private bool IsAadCertificate(X509Certificate2 signingCertificate)
        //{
        //    X500DistinguishedName dn1 = new X500DistinguishedName(signingCertificate.IssuerName.Format(false).Replace('+', ','));

        //    foreach (string dn in this.azureAdOptions.Value.AadIssuerDNs)
        //    {
        //        if (dn1.IsDnMatch(dn))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        private async Task<TokenResponse> ValidateAadAssertionAsync(X509Certificate2 signingCertificate, string tenantId, string deviceId)
        {
            this.logger.LogTrace($"Client has presented an Azure AD certificate for authentication of device {deviceId} in tenant {tenantId}");

            if (!Guid.TryParse(tenantId, out _))
            {
                throw new SecurityTokenValidationException("The tenant ID provided in the token was not in the correct format");
            }

            if (!Guid.TryParse(deviceId, out _))
            {
                throw new SecurityTokenValidationException("The device ID provided in the token was not in the correct format");
            }

            Microsoft.Graph.Device aadDevice = await this.graphProvider.GetAadDeviceByDeviceIdAsync(tenantId, deviceId);

            if (!aadDevice.HasDeviceThumbprint(signingCertificate.Thumbprint))
            {
                throw new AadDeviceNotFoundException($"The certificate thumbprint '{signingCertificate.Thumbprint}' could not be found on device {deviceId} in the Azure Active Directory");
            }

            aadDevice.ThrowOnDeviceDisabled();

            switch (aadDevice.TrustType.ToLowerInvariant())
            {
                case "azuread":
                    if (!this.agentOptions.CurrentValue.AllowAzureAdJoinedDeviceAuth)
                    {
                        throw new UnsupportedAuthenticationTypeException("The device is Azure AD joined, but Azure AD-joined devices are not permitted to authenticate to the system");
                    }
                    break;

                case "workplace":
                    if (!this.agentOptions.CurrentValue.AllowAzureAdRegisteredDeviceAuth)
                    {
                        throw new UnsupportedAuthenticationTypeException("The device is Azure AD registered, but Azure AD-registered devices are not permitted to authenticate to the system");
                    }
                    break;

                default:
                    throw new UnsupportedAuthenticationTypeException($"The AAD device has an unknown trust type '{aadDevice.TrustType}'");
            }

            Device device = await this.devices.GetOrCreateDeviceAsync(aadDevice, tenantId);
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
