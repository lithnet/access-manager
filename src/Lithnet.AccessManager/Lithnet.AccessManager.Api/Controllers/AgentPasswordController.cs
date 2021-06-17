using Lithnet.AccessManager.Api.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;
using Lithnet.AccessManager.Server;
using Microsoft.AspNetCore.Http;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("agent/password")]
    [Produces("application/json")]
    [Authorize("ComputersOnly", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AgentPasswordController : Controller
    {
        private readonly ILogger<AgentPasswordController> logger;
        private readonly IDbDevicePasswordProvider passwordProvider;
        private readonly ICertificateProvider certificateProvider;
        private readonly IApiErrorResponseProvider errorProvider;
        private readonly IEncryptionProvider encryptionProvider;

        private X509Certificate2 encryptionCertificate;

        public AgentPasswordController(ILogger<AgentPasswordController> logger, IDbDevicePasswordProvider passwordProvider, ICertificateProvider certificateProvider, IApiErrorResponseProvider errorProvider, IEncryptionProvider encryptionProvider)
        {
            this.logger = logger;
            this.passwordProvider = passwordProvider;
            this.certificateProvider = certificateProvider;
            this.errorProvider = errorProvider;
            this.encryptionProvider = encryptionProvider;
        }

        [HttpGet()]
        public async Task<IActionResult> GetAsync()
        {
            try
            {
                string deviceId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (deviceId == null)
                {
                    throw new BadRequestException("The device ID was not found in the claim");
                }

                this.logger.LogTrace($"Checking to see if device {deviceId} requires a password change");
                if (await this.passwordProvider.HasPasswordExpired(deviceId))
                {
                    this.logger.LogTrace($"Device {deviceId} requires a password change");

                    return this.StatusCode(StatusCodes.Status205ResetContent,
                        new PasswordGetResponse
                        {
                            EncryptionCertificateThumbprint = this.EncryptionCertificate.Thumbprint
                        });
                }
                else
                {
                    this.logger.LogTrace($"Device {deviceId} does not require a password change");
                    return this.NoContent();
                }
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        [HttpPost()]
        public async Task<IActionResult> UpdateAsync([FromBody] PasswordUpdateRequest request)
        {
            try
            {
                this.ValidatePasswordUpdateRequest(request);

                string deviceId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (deviceId == null)
                {
                    throw new BadRequestException("The device ID was not found in the claim");
                }

                this.logger.LogTrace($"Attempting update for device {deviceId} ");

                string passwordId = await this.passwordProvider.UpdateDevicePassword(deviceId, request);

                this.logger.LogInformation($"Successfully updated password for device {deviceId}. Password ID {passwordId}");

                return this.Json(new PasswordUpdateResponse { PasswordId = passwordId });
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        [HttpDelete("{requestId}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] string requestId)
        {
            try
            {
                string deviceId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (deviceId == null)
                {
                    throw new BadRequestException("The device ID was not found in the claim");
                }

                this.logger.LogTrace($"Attempting to rollback password ID {requestId} for device {deviceId} ");

                await this.passwordProvider.RevertLastPasswordChange(deviceId, requestId);

                this.logger.LogInformation($"Successfully rolled-back password ID {requestId} for device {deviceId} ");

                return this.Ok();
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }

        private void ValidatePasswordUpdateRequest(PasswordUpdateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.ExpiryDate.Ticks == 0)
            {
                throw new BadRequestException("The request did not provide an expiry date");
            }

            if (string.IsNullOrWhiteSpace(request.AccountName))
            {
                throw new BadRequestException("The request did not supply an account name");
            }

            if (string.IsNullOrWhiteSpace(request.PasswordData))
            {
                throw new BadRequestException("The request did not supply any password data");
            }

            try
            {
                this.encryptionProvider.Decrypt(request.PasswordData, x => this.certificateProvider.FindDecryptionCertificate(x));
            }
            catch (Exception ex)
            {
                throw new BadRequestException("Could not decrypt the password data provided by the client", ex);
            }
        }

        private X509Certificate2 EncryptionCertificate
        {
            get
            {
                if (this.encryptionCertificate == null)
                {
                    this.encryptionCertificate = this.certificateProvider.FindEncryptionCertificate();
                }

                return this.encryptionCertificate;
            }
        }

    }
}
