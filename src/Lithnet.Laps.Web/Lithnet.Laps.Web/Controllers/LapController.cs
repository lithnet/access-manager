using System;
using System.ComponentModel;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.Models;
using NLog;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IAuthorizationService = Lithnet.Laps.Web.Authorization.IAuthorizationService;
using System.Globalization;
using Lithnet.Laps.Web.Exceptions;

namespace Lithnet.Laps.Web.Controllers
{
    [Authorize]
    [Localizable(true)]
    public class LapController : Controller
    {
        private readonly IAuthorizationService authorizationService;
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly IAuditEventProcessor reporting;
        private readonly IRateLimiter rateLimiter;
        private readonly IUserInterfaceSettings userInterfaceSettings;

        public LapController(IAuthorizationService authorizationService, ILogger logger, IDirectory directory,
            IAuditEventProcessor reporting, IRateLimiter rateLimiter, IUserInterfaceSettings userInterfaceSettings)
        {
            this.authorizationService = authorizationService;
            this.logger = logger;
            this.directory = directory;
            this.reporting = reporting;
            this.rateLimiter = rateLimiter;
            this.userInterfaceSettings = userInterfaceSettings;
        }

        public IActionResult Get()
        {
            return this.View(new LapRequestModel
            {
                ShowReason = this.userInterfaceSettings.UserSuppliedReason != AuditReasonFieldState.Hidden,
                ReasonRequired = this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Get(LapRequestModel model)
        {
            model.ShowReason = this.userInterfaceSettings.UserSuppliedReason != AuditReasonFieldState.Hidden;
            model.ReasonRequired = this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required;

            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            IUser user = null;

            try
            {
                model.FailureReason = null;

                if (string.IsNullOrWhiteSpace(model.UserRequestReason) && this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required)
                {
                    logger.LogEventError(EventIDs.ReasonRequired, string.Format(LogMessages.ReasonRequired, user.SamAccountName));

                    model.FailureReason = UIMessages.ReasonRequired;
                    return this.View("Get", model);
                }

                try
                {
                    user = this.HttpContext.GetLoggedInUser(this.directory) ?? throw new NotFoundException();
                }
                catch (NotFoundException ex)
                {
                    this.logger.LogEventError(EventIDs.SsoIdentityNotFound, null, ex);

                    model.FailureReason = UIMessages.SsoIdentityNotFound;
                    return this.View("Get", model);
                }

                var rateLimitResult = this.rateLimiter.GetRateLimitResult(user.Sid.ToString(), this.Request);

                if (rateLimitResult.IsRateLimitExceeded)
                {
                    this.LogRateLimitEvent(model, user, rateLimitResult);
                    return this.View("RateLimitExceeded");
                }

                this.logger.LogEventSuccess(EventIDs.UserRequestedPassword, string.Format(LogMessages.UserHasRequestedPassword, user.SamAccountName, model.ComputerName));

                IComputer computer;

                try
                {
                    computer = this.directory.GetComputer(model.ComputerName) ?? throw new NotFoundException();
                }
                catch (AmbiguousNameException ex)
                {
                    this.logger.LogEventError(EventIDs.ComputerNameAmbiguous, string.Format(LogMessages.ComputerNameAmbiguous, user.SamAccountName, model.ComputerName), ex);

                    model.FailureReason = UIMessages.ComputerNameAmbiguous;
                    return this.View("Get", model);
                }
                catch (NotFoundException ex)
                {
                    this.logger.LogEventError(EventIDs.ComputerNotFound, string.Format(LogMessages.ComputerNotFoundInDirectory, user.SamAccountName, model.ComputerName), ex);

                    model.FailureReason = UIMessages.ComputerNotFoundInDirectory;
                    return this.View("Get", model);
                }

                // Do authorization check first.

                AuthorizationResponse authResponse = this.authorizationService.GetAuthorizationResponse(user, computer);

                if (!authResponse.IsAuthorized())
                {
                    this.AuditAuthZFailure(model, authResponse, user, computer);
                    model.FailureReason = UIMessages.NotAuthorized;
                    return this.View("Get", model);
                }

                // Do actual work only if authorized.

                PasswordData passwordData = this.directory.GetPassword(computer);

                if (passwordData == null)
                {
                    this.logger.LogEventError(EventIDs.LapsPasswordNotPresent, string.Format(LogMessages.NoLapsPassword, computer.SamAccountName, user.SamAccountName));

                    model.FailureReason = UIMessages.NoLapsPassword;
                    return this.View("Get", model);
                }

                if (authResponse.ExpireAfter.Ticks > 0)
                {
                    this.UpdateTargetPasswordExpiry(authResponse.ExpireAfter, computer);

                    // Get the password again with the updated expiry date.
                    passwordData = this.directory.GetPassword(computer);
                }

                this.reporting.GenerateAuditEvent(new AuditableAction
                {
                    AuthzResponse = authResponse,
                    RequestModel = model,
                    IsSuccess = true,
                    User = user,
                    Computer = computer,
                    EventID = EventIDs.PasswordAccessed,
                    ComputerExpiryDate = passwordData.ExpirationTime?.ToString(CultureInfo.CurrentUICulture)
                });

                return this.View("Show", new LapEntryModel(computer, passwordData));
            }
            catch (AuditLogFailureException ex)
            {
                this.logger.LogEventError(EventIDs.AuthZFailedAuditError, string.Format(LogMessages.AuthZFailedAuditError, user?.SamAccountName ?? LogMessages.UnknownComputerPlaceholder, model.ComputerName), ex);

                model.FailureReason = UIMessages.AccessDenied;
                return this.View("Get", model);
            }
            catch (Exception ex)
            {
                this.logger.LogEventError(EventIDs.UnexpectedError, string.Format(LogMessages.UnhandledError, model.ComputerName, user?.SamAccountName ?? LogMessages.UnknownComputerPlaceholder), ex);

                model.FailureReason = UIMessages.UnexpectedError;
                return this.View("Get", model);
            }
        }

        private void LogRateLimitEvent(LapRequestModel model, IUser user, RateLimitResult rateLimitResult)
        {
            AuditableAction action = new AuditableAction
            {
                User = user,
                IsSuccess = false,
                RequestModel = model,
            };

            if (rateLimitResult.IsUserRateLimit)
            {
                action.EventID = EventIDs.RateLimitExceededUser;
                action.Message = string.Format(LogMessages.RateLimitExceededUser, user.SamAccountName, rateLimitResult.IPAddress, rateLimitResult.Threshold, rateLimitResult.Duration);
            }
            else
            {
                action.EventID = EventIDs.RateLimitExceededIP;
                action.Message = string.Format(LogMessages.RateLimitExceededIP, user.SamAccountName, rateLimitResult.IPAddress, rateLimitResult.Threshold, rateLimitResult.Duration);
            }

            this.reporting.GenerateAuditEvent(action);
        }

        private void AuditAuthZFailure(LapRequestModel model, AuthorizationResponse authorizationResponse, IUser user, IComputer computer)
        {
            AuditableAction action = new AuditableAction
            {
                AuthzResponse = authorizationResponse,
                User = user,
                Computer = computer,
                IsSuccess = false,
                RequestModel = model,
                Message = string.Format(LogMessages.AuthorizationFailed, user.SamAccountName, model.ComputerName)
            };

            action.EventID = authorizationResponse.Code switch
            {
                AuthorizationResponseCode.NoMatchingRuleForComputer => EventIDs.AuthZFailedNoTargetMatch,
                AuthorizationResponseCode.NoMatchingRuleForUser => EventIDs.AuthZFailedNoReaderPrincipalMatch,
                AuthorizationResponseCode.ExplicitlyDenied => EventIDs.AuthZExplicitlyDenied,
                _ => EventIDs.AuthZFailed,
            };

            this.reporting.GenerateAuditEvent(action);
        }

        [Localizable(false)]
        private void UpdateTargetPasswordExpiry(TimeSpan expireAfter, IComputer computer)
        {
            this.logger.Trace($"Target rule requires password to change after {expireAfter}");
            DateTime newDateTime = DateTime.UtcNow.Add(expireAfter);
            this.directory.SetPasswordExpiryTime(computer, newDateTime);
            this.logger.Trace($"Set expiry time for {computer.SamAccountName} to {newDateTime.ToLocalTime()}");
        }
    }
}