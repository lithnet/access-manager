using Lithnet.AccessManager.Api.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("agent/password")]
    [Authorize("ComputersOnly", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AgentPasswordController : Controller
    {
        private readonly ILogger<AgentPasswordController> logger;
        private readonly IDbDevicePasswordProvider passwordProvider;

        public AgentPasswordController(ILogger<AgentPasswordController> logger, IDbDevicePasswordProvider passwordProvider)
        {
            this.logger = logger;
            this.passwordProvider = passwordProvider;
        }

        [HttpGet()]
        public async Task<IActionResult> Get()
        {
            try
            {
                string deviceId = "abs";// this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (deviceId == null)
                {
                    throw new BadRequestException("The device ID was not found in the claim");
                }

                this.logger.LogTrace($"Checking to see if device {deviceId} requires a password change");
                if (await this.passwordProvider.HasPasswordExpired(deviceId))
                {
                    this.logger.LogTrace($"Device {deviceId} requires a password change");
                    return this.StatusCode(StatusCodes.Status205ResetContent);
                }
                else
                {
                    this.logger.LogTrace($"Device {deviceId} does not require a password change");
                    return this.NoContent();
                }
            }
            catch (BadRequestException ex)
            {
                this.logger.LogError(ex, "The request could not be processed due to an input error");
                return this.BadRequest();
            }
            catch (ObjectNotFoundException ex)
            {
                this.logger.LogError(ex, "The device could not be found");
                return this.BadRequest();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The request could not be processed");
                throw;
            }
        }

        [HttpPost()]
        public async Task<IActionResult> Update([FromBody] PasswordUpdateRequest request)
        {
            try
            {
                string deviceId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (deviceId == null)
                {
                    throw new BadRequestException("The device ID was not found in the claim");
                }

                string requestId = await this.passwordProvider.UpdateDevicePassword(deviceId, request);

                return this.Json(new { request_id = requestId });
            }
            catch (BadRequestException ex)
            {
                this.logger.LogError(ex, "The request could not be processed due to an input error");
                return this.BadRequest();
            }
            catch (ObjectNotFoundException ex)
            {
                this.logger.LogError(ex, "The device could not be found");
                return this.BadRequest();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The request could not be processed");
                throw;
            }
        }

        [HttpDelete("{requestId}")]
        public async Task<IActionResult> Delete([FromRoute] string requestId)
        {
            string deviceId = null;

            try
            {
                deviceId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (deviceId == null)
                {
                    throw new BadRequestException("The device ID was not found in the claim");
                }

                await this.passwordProvider.RevertLastPasswordChange(deviceId, requestId);

                return this.Ok();
            }
            catch (PasswordRollbackDeniedException ex)
            {
                this.logger.LogError(ex, $"The request to rollback password request Id {requestId} for device {deviceId} was denied");
                return this.Forbid(JwtBearerDefaults.AuthenticationScheme);
            }
            catch (BadRequestException ex)
            {
                this.logger.LogError(ex, "The request could not be processed due to an input error");
                return this.BadRequest();
            }
            catch (ObjectNotFoundException ex)
            {
                this.logger.LogError(ex, "The device could not be found");
                return this.BadRequest();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The request could not be processed");
                throw;
            }
        }
    }
}
