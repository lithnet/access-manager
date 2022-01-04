using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Service.App_LocalResources;
using Lithnet.AccessManager.Service.AppSettings;
using Lithnet.AccessManager.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.App_LocalResources;

namespace Lithnet.AccessManager.Service.Controllers
{
    [Authorize(Policy = "RequireAuthorizedUser")]
    [Localizable(true)]
    public class RoleController : Controller
    {
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly ILogger logger;
        private readonly UserInterfaceOptions userInterfaceSettings;
        private readonly RoleFulfillmentService fulfillmentService;
        private readonly IAmsLicenseManager licenseManager;

        public RoleController(ILogger<RoleAuthorizationService> logger, IOptionsSnapshot<UserInterfaceOptions> userInterfaceSettings, IAuthenticationProvider authenticationProvider, RoleFulfillmentService fulfillmentService, IAmsLicenseManager licenseManager)
        {
            this.logger = logger;
            this.userInterfaceSettings = userInterfaceSettings.Value;
            this.authenticationProvider = authenticationProvider;
            this.licenseManager = licenseManager;
            this.fulfillmentService = fulfillmentService;
        }

        [HttpGet]
        public async Task<IActionResult> AccessRequest()
        {
            RoleRequestModel model = new RoleRequestModel();

            IActiveDirectoryUser user = null;

            try
            {
                if (!this.GetUser(out user, out IActionResult failure))
                {
                    return failure;
                }

                await this.AddRolesToModel(user, model);

                return this.View(model);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UnexpectedError, ex, string.Format(LogMessages.UnhandledError, AccessMask.Jit, null, user?.MsDsPrincipalName));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.UnexpectedError
                });
            }
        }

        private async Task AddRolesToModel(IActiveDirectoryUser user, RoleRequestModel model)
        {
            var roles = await this.fulfillmentService.GetAvailableRoles(user);
            model.AvailableRoles = roles;
            model.SelectionItems = roles.Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(t.Name, t.Key)).OrderBy(t => t.Text).ToList();
        }

        [HttpGet]
        public IActionResult AccessResponse()
        {
            return this.RedirectToAction("AccessRequest");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AccessResponse(RoleRequestModel model)
        {
            IActiveDirectoryUser user = null;

            try
            {
                if (!this.GetUser(out user, out IActionResult failure))
                {
                    return failure;
                }

                await this.AddRolesToModel(user, model);

                if (!this.ModelState.IsValid)
                {
                    return this.View("AccessRequest", model);
                }

                model.FailureReason = null;

                this.logger.LogInformation(EventIDs.UserRequestedAccessToComputer, string.Format(LogMessages.UserHasRequestedAccessToComputer, user.MsDsPrincipalName, model.SelectedRoleKey, AccessMask.Jit));

                if (!ValidateRequestReason(model, user, out IActionResult actionResult))
                {
                    return actionResult;
                }

                return await GetAuthorizationResponseAsync(model, user);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UnexpectedError, ex, string.Format(LogMessages.UnhandledError, AccessMask.Jit, null, user?.MsDsPrincipalName));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.UnexpectedError
                });
            }
        }

        private async Task<IActionResult> GetAuthorizationResponseAsync(RoleRequestModel model, IActiveDirectoryUser user)
        {
            try
            {
                var result = await this.fulfillmentService.RequestRole(user, model.SelectedRoleKey, this.Request.HttpContext.Connection.RemoteIpAddress, model.UserRequestReason, model.RequestedDuration);

                if (result.IsSuccess)
                {
                    var jitDetails = new JitDetailsModel(result.RoleName, user.MsDsPrincipalName, result.Expiry);
                    return this.View("AccessResponseJit", jitDetails);
                }
                else
                {
                    return this.View("AccessRequestError", this.GenerateAuthZFailureModel(result));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.AuthZError, ex, string.Format(LogMessages.AuthZError, user.MsDsPrincipalName));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.AuthZError
                });
            }
        }

        private ErrorModel GenerateAuthZFailureModel(RoleFulfillmentResult fulfillmentResult)
        {
            return new ErrorModel
            {
                Heading = UIMessages.AccessDenied,
                Message = fulfillmentResult.Error switch
                {
                    RoleFulfillmentError.RateLimitExceeded => UIMessages.RateLimitError,
                    _ => UIMessages.NotAuthorized
                }
            };
        }

        private bool GetUser(out IActiveDirectoryUser user, out IActionResult failure)
        {
            failure = null;

            try
            {
                user = this.authenticationProvider.GetLoggedInUser() ?? throw new ObjectNotFoundException();
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.IdentityDiscoveryError, ex, LogMessages.IdentityDiscoveryError);
                user = null;

                ErrorModel model = new ErrorModel
                {
                    Heading = UIMessages.AccessDenied,
                    Message = UIMessages.IdentityDiscoveryError,
                };

                failure = this.View("AccessRequestError", model);
                return false;
            }
        }

        private bool ValidateRequestReason(RoleRequestModel model, IActiveDirectoryUser user, out IActionResult actionResult)
        {
            actionResult = null;

            if (string.IsNullOrWhiteSpace(model.UserRequestReason) && this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required)
            {
                logger.LogError(EventIDs.ReasonRequired, string.Format(LogMessages.ReasonRequired, user.MsDsPrincipalName));
                model.FailureReason = UIMessages.ReasonRequired;
                actionResult = this.View("AccessRequest", model);
                return false;
            }

            return true;
        }

        private IActionResult HandleNoRoles()
        {
            ErrorModel model = new ErrorModel
            {
                Heading = "No roles",
                Message = "Not authorized",
            };

            return this.View("AccessRequestError", model);
        }
    }
}