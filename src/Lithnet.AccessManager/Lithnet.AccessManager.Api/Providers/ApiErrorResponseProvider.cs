using Lithnet.AccessManager.Api.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using Lithnet.AccessManager.Server;

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
                    return new BadRequestObjectResult(new { Error = new ApiError(ApiConstants.BadRequest, "The request was invalid or incorrectly constructed") });

                case SecurityTokenValidationException _:
                    this.logger.LogError(ex, "The security token failed the validation process");
                    return new UnauthorizedObjectResult(new { Error = new ApiError(ApiConstants.TokenValidationFailed, "The security token failed to validate") });

                case DeviceCredentialsNotFoundException _:
                    this.logger.LogError(ex, "The device credentials could not be found in the AMS database");
                    return new UnauthorizedObjectResult(new { Error = new ApiError(ApiConstants.DeviceCredentialsNotFound, "The device credentials are not valid") });

                case DeviceNotFoundException _:
                    this.logger.LogError(ex, "The device could not be found in the AMS database");
                    return new UnauthorizedObjectResult(new { Error = new ApiError(ApiConstants.DeviceNotFound, "The device is not registered") });

                case DeviceDisabledException _:
                    this.logger.LogError(ex, "The device was disabled");
                    return new UnauthorizedObjectResult(new { Error = new ApiError(ApiConstants.DeviceDisabled, "The device is disabled") });

                case AadObjectNotFoundException _:
                    this.logger.LogError(ex, "The device or a record of its certificate could not be found in AAD");
                    return new UnauthorizedObjectResult(new { Error = new ApiError(ApiConstants.AadDeviceNotFound, "The device is not registered in AAD") });

                case UnsupportedAuthenticationTypeException _:
                    this.logger.LogError(ex, "The device requested an unsupported authentication type");
                    return new UnauthorizedObjectResult(new { Error = new ApiError(ApiConstants.UnsupportedAuthType, "The request authentication type is not supported") });

                case PasswordRollbackDeniedException _:
                    this.logger.LogError(ex, "The request to rollback password a password was denied");
                    return new JsonResult(new { Error = new ApiError(ApiConstants.RollbackDenied, "The request to rollback the password was denied") })
                    {
                        StatusCode = StatusCodes.Status410Gone
                    };

                case DeviceNotApprovedException _:
                    this.logger.LogError(ex, "The device was not approved");
                    return new UnauthorizedObjectResult(new { Error = new ApiError(ApiConstants.DeviceNotApproved, "The device is not approved") });

                case RegistrationDisabledException _:
                    this.logger.LogError(ex, "A client requested registration, but registration was disabled on this server");
                    return new JsonResult(new { Error = new ApiError(ApiConstants.RegistrationDisabled, "Registration is disabled") })
                    {
                        StatusCode = StatusCodes.Status406NotAcceptable
                    };

                case RegistrationKeyValidationException _:
                    this.logger.LogError(ex, "The registration key could not be validated");
                    return new JsonResult(new { Error = new ApiError(ApiConstants.InvalidRegistrationKey, "The registration key was not accepted") })
                    {
                        StatusCode = StatusCodes.Status412PreconditionFailed
                    };

                default:
                    this.logger.LogError(ex, "The request could not be processed");
                    return new JsonResult(new { Error = new ApiError(ApiConstants.InternalError, "An internal error occurred and the request could not be processed") })
                    {
                        StatusCode = StatusCodes.Status500InternalServerError
                    };
            }
        }
    }
}
