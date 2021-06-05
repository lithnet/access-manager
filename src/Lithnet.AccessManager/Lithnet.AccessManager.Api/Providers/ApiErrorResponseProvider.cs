using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Lithnet.AccessManager.Api
{
    public class ApiErrorResponseProvider : IApiErrorResponseProvider
    {
        private readonly ILogger<ApiErrorResponseProvider> logger;

        public ApiErrorResponseProvider(ILogger<ApiErrorResponseProvider> logger)
        {
            this.logger = logger;
        }

        public IActionResult GetErrorResult(Exception ex)
        {
            switch (ex)
            {
                case BadRequestException _:
                    this.logger.LogError(ex, "The request could not be processed due to an input error");
                    return new BadRequestObjectResult(new { Error = new ApiError("bad-request", "The request was invalid or incorrectly constructed") });

                case SecurityTokenValidationException _:
                    this.logger.LogError(ex, "The security token failed the validation process");
                    return new UnauthorizedObjectResult(new { Error = new ApiError("token-validation-failed", "The security token failed to validate") });

                case DeviceNotFoundException _:
                    this.logger.LogError(ex, "The device could not be found in the AMS database");
                    return new UnauthorizedObjectResult(new { Error = new ApiError("not-registered", "The device is not registered") });

                case AadDeviceNotFoundException _:
                    this.logger.LogError(ex, "The device or a record of its certificate could not be found in AAD");
                    return new UnauthorizedObjectResult(new { Error = new ApiError("not-registered-aad", "The device is not registered in AAD") });

                case UnsupportedAuthenticationTypeException _:
                    this.logger.LogError(ex, "The device requested an unsupported authentication type");
                    return new UnauthorizedObjectResult(new { Error = new ApiError("unsupported-auth-type", "The request authentication type is not supported") });

                case PasswordRollbackDeniedException _:
                    this.logger.LogError(ex, "The request to rollback password a password was denied");
                    return new JsonResult(new { Error = new ApiError("rollback-denied", "The request to rollback the password was denied") })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };

                default:
                    this.logger.LogError(ex, "The request could not be processed");
                    return new JsonResult(new { Error = new ApiError("internal-error", "An internal error occurred and the request could not be processed") })
                    {
                        StatusCode = StatusCodes.Status500InternalServerError
                    };
            }
        }
    }
}
