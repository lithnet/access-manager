using System;
using System.ComponentModel;
using System.Web.Mvc;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.Models;
using NLog;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web.Controllers
{
    [Authorize]
    [Localizable(true)]
    public class LapController : Controller
    {
        private readonly IAuthorizationService authorizationService;
        private readonly ILogger logger;
        private readonly IDirectory directory;
        private readonly IReporting reporting;
        private readonly IRateLimiter rateLimiter;
        private readonly IAuthenticationService authenticationService;
        private readonly IUserInterfaceSettings userInterfaceSettings;

        public LapController(IAuthorizationService authorizationService, ILogger logger, IDirectory directory,
            IReporting reporting, IRateLimiter rateLimiter,
            IAuthenticationService authenticationService, IUserInterfaceSettings userInterfaceSettings)
        {
            this.authorizationService = authorizationService;
            this.logger = logger;
            this.directory = directory;
            this.reporting = reporting;
            this.rateLimiter = rateLimiter;
            this.authenticationService = authenticationService;
            this.userInterfaceSettings = userInterfaceSettings;
        }

        public ActionResult Get()
        {
            return this.View(new LapRequestModel
            {
                ShowReason = this.userInterfaceSettings.UserSuppliedReason != AuditReasonFieldState.Hidden,
                ReasonRequired = this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Get(LapRequestModel model)
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
                    return this.LogAndReturnErrorResponse(model, UIMessages.ReasonRequired, EventIDs.ReasonRequired);
                }

                try
                {
                    user = this.authenticationService.GetLoggedInUser(this.directory) ?? throw new NotFoundException();
                }
                catch (NotFoundException ex)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.SsoIdentityNotFound, EventIDs.SsoIdentityNotFound, null, ex);
                }

                var rateLimitResult = this.rateLimiter.GetRateLimitResult(user.Sid.ToString(), this.Request);

                if (rateLimitResult.IsRateLimitExceeded)
                {
                    this.LogRateLimitEvent(model, user, rateLimitResult);
                    return this.View("RateLimitExceeded");
                }

                this.reporting.LogSuccessEvent(EventIDs.UserRequestedPassword, string.Format(LogMessages.UserHasRequestedPassword, user.SamAccountName, model.ComputerName));

                IComputer computer;

                try
                {
                    computer = this.directory.GetComputer(model.ComputerName) ?? throw new NotFoundException();
                }
                catch (AmbiguousNameException ex)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.ComputerNameAmbiguous, EventIDs.ComputerNameAmbiguous, string.Format(LogMessages.ComputerNameAmbiguous, user.SamAccountName, model.ComputerName), ex);
                }
                catch (NotFoundException ex)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.ComputerNotFoundInDirectory, EventIDs.ComputerNotFound, string.Format(LogMessages.ComputerNotFoundInDirectory, user.SamAccountName, model.ComputerName), ex);
                }

                // Do authorization check first.

                AuthorizationResponse authResponse = this.authorizationService.GetAuthorizationResponse(user, computer);

                if (!authResponse.IsAuthorized())
                {
                    return this.LogAndResponseToAuthZFailure(model, authResponse, user, computer);
                }

                // Do actual work only if authorized.

                PasswordData passwordData = this.directory.GetPassword(computer);

                if (passwordData == null)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.NoLapsPassword, EventIDs.LapsPasswordNotPresent, string.Format(LogMessages.NoLapsPassword, computer.SamAccountName, user.SamAccountName));
                }

                if (authResponse.ExpireAfter.Ticks > 0)
                {
                    this.UpdateTargetPasswordExpiry(authResponse.ExpireAfter, computer);

                    // Get the password again with the updated expiry date.
                    passwordData = this.directory.GetPassword(computer);
                }

                this.reporting.PerformAuditSuccessActions(model, authResponse, user, computer, passwordData);

                return this.View("Show", new LapEntryModel(computer, passwordData));
            }
            catch (Exception ex)
            {
                return this.LogAndReturnErrorResponse(model, UIMessages.UnexpectedError, EventIDs.UnexpectedError, string.Format(LogMessages.UnhandledError, model.ComputerName, user?.SamAccountName ?? LogMessages.UnknownComputerPlaceholder), ex);
            }
        }

        private void LogRateLimitEvent(LapRequestModel model, IUser user, RateLimitResult rateLimitResult)
        {
            if (rateLimitResult.IsUserRateLimit)
            {
                this.reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededUser,
               string.Format(LogMessages.RateLimitExceededUser, user.SamAccountName, rateLimitResult.IPAddress, rateLimitResult.Threshold, rateLimitResult.Duration), null, null, user, null);
            }
            else
            {
                this.reporting.PerformAuditFailureActions(model, UIMessages.RateLimitError, EventIDs.RateLimitExceededIP,
           string.Format(LogMessages.RateLimitExceededIP, user.SamAccountName, rateLimitResult.IPAddress, rateLimitResult.Threshold, rateLimitResult.Duration), null, null, user, null);
            }
        }

        private ViewResult LogAndResponseToAuthZFailure(LapRequestModel model, AuthorizationResponse authorizationResponse, IUser user, IComputer computer)
        {
            int eventID;

            switch (authorizationResponse.Code)
            {
                case AuthorizationResponseCode.NoMatchingRuleForComputer:
                    eventID = EventIDs.AuthZFailedNoTargetMatch;
                    break;
                case AuthorizationResponseCode.NoMatchingRuleForUser:
                    eventID = EventIDs.AuthZFailedNoReaderPrincipalMatch;
                    break;
                case AuthorizationResponseCode.ExplicitlyDenied:
                    eventID = EventIDs.AuthZExplicitlyDenied;
                    break;
                default:
                    eventID = EventIDs.AuthZFailed;
                    break;
            }

            return this.AuditAndReturnErrorResponse(model: model, userMessage: UIMessages.NotAuthorized, eventID: eventID, logMessage: string.Format(LogMessages.AuthorizationFailed, user.SamAccountName, model.ComputerName), user: user, computer: computer, authorizationResponse: authorizationResponse);
        }

        private ViewResult AuditAndReturnErrorResponse(LapRequestModel model, string userMessage, int eventID, string logMessage = null, Exception ex = null, AuthorizationResponse authorizationResponse = null, IUser user = null, IComputer computer = null)
        {
            this.reporting.PerformAuditFailureActions(model, userMessage, eventID, logMessage, ex, authorizationResponse, user, computer);
            model.FailureReason = userMessage;
            return this.View("Get", model);
        }

        private ViewResult LogAndReturnErrorResponse(LapRequestModel model, string userMessage, int eventID, string logMessage = null, Exception ex = null)
        {
            this.reporting.LogErrorEvent(eventID, logMessage ?? userMessage, ex);
            model.FailureReason = userMessage;
            return this.View("Get", model);
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