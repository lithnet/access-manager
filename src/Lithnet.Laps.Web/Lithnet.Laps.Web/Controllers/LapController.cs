using System;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.Web.Mvc;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authentication;
using Lithnet.Laps.Web.Security.Authorization;
using NLog;

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
        private readonly IAvailableTargets availableTargets;
        private readonly IAuthenticationService authenticationService;
        private readonly ILapsConfig config;

        public LapController(IAuthorizationService authorizationService, ILogger logger, IDirectory directory,
            IReporting reporting, IRateLimiter rateLimiter, IAvailableTargets availableTargets,
            IAuthenticationService authenticationService, ILapsConfig config)
        {
            this.authorizationService = authorizationService;
            this.logger = logger;
            this.directory = directory;
            this.reporting = reporting;
            this.rateLimiter = rateLimiter;
            this.availableTargets = availableTargets;
            this.authenticationService = authenticationService;
            this.config = config;
        }

        public ActionResult Get()
        {
            return this.View(new LapRequestModel {ShowReason = this.config.Audit.Reason != ConfigSection.AuditReasonFieldState.NotRequired});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Get(LapRequestModel model)
        {
            model.ShowReason = this.config.Audit.Reason != ConfigSection.AuditReasonFieldState.NotRequired;

            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            IUser user = null;

            try
            {
                model.FailureReason = null;

                if (string.IsNullOrWhiteSpace(model.UserRequestReason) && this.config.Audit.Reason == ConfigSection.AuditReasonFieldState.Required)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.ReasonRequired, EventIDs.ReasonRequired);
                }

                try
                {
                    user = this.authenticationService.GetLoggedInUser(this.directory);

                    if (user == null)
                    {
                        throw new NoMatchingPrincipalException();
                    }
                }
                catch (NoMatchingPrincipalException ex)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.SsoIdentityNotFound, EventIDs.SsoIdentityNotFound, null, ex);
                }

                if (this.rateLimiter.IsRateLimitExceeded(model, user, this.Request))
                {
                    return this.View("RateLimitExceeded");
                }

                this.reporting.LogSuccessEvent(EventIDs.UserRequestedPassword, string.Format(LogMessages.UserHasRequestedPassword, user.SamAccountName, model.ComputerName));

                IComputer computer;

                try
                {
                    computer = this.directory.GetComputer(model.ComputerName);

                    if (computer == null)
                    {
                        throw new NotFoundException();
                    }
                }
                catch (AmbiguousNameException ex)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.ComputerNameAmbiguous, EventIDs.ComputerNameAmbiguous, string.Format(LogMessages.ComputerNameAmbiguous, user.SamAccountName, model.ComputerName), ex);
                }
                catch (NotFoundException ex)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.ComputerNotFoundInDirectory, EventIDs.ComputerNotFound, string.Format(LogMessages.ComputerNotFoundInDirectory, user.SamAccountName, model.ComputerName), ex);

                }

                // Is a target configured?

                ITarget target = this.availableTargets.GetMatchingTargetOrNull(computer);

                if (target == null)
                {
                    return this.AuditAndReturnErrorResponse(model, UIMessages.NotAuthorized, EventIDs.AuthZFailedNoTargetMatch, string.Format(LogMessages.NoTargetsExist, user.SamAccountName, model.ComputerName), user: user, computer: computer);
                }

                // Do authorization check first.

                AuthorizationResponse authResponse = this.authorizationService.CanAccessPassword(user, computer, target);

                if (!authResponse.IsAuthorized)
                {
                    return this.AuditAndReturnErrorResponse(model: model, userMessage: UIMessages.NotAuthorized, eventID: EventIDs.AuthorizationFailed, logMessage: string.Format(LogMessages.AuthorizationFailed, user.SamAccountName, model.ComputerName), user: user, computer: computer, target: target);
                }

                // Do actual work only if authorized.

                PasswordData passwordData = this.directory.GetPassword(computer);

                if (passwordData == null)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.NoLapsPassword, EventIDs.LapsPasswordNotPresent, string.Format(LogMessages.NoLapsPassword, computer.SamAccountName, user.SamAccountName));
                }

                if (target.ExpireAfter.Ticks > 0)
                {
                    this.UpdateTargetPasswordExpiry(target, computer);

                    // Get the password again with the updated expiry date.
                    passwordData = this.directory.GetPassword(computer);
                }

                this.reporting.PerformAuditSuccessActions(model, target, authResponse, user, computer, passwordData);

                return this.View("Show", new LapEntryModel(computer, passwordData));
            }
            catch (Exception ex)
            {
                return this.LogAndReturnErrorResponse(model, UIMessages.UnexpectedError, EventIDs.UnexpectedError, string.Format(LogMessages.UnhandledError, model.ComputerName, user?.SamAccountName ?? LogMessages.UnknownComputerPlaceholder), ex);
            }
        }

        private ViewResult AuditAndReturnErrorResponse(LapRequestModel model, string userMessage, int eventID, string logMessage = null, Exception ex = null, ITarget target = null, AuthorizationResponse authorizationResponse = null, IUser user = null, IComputer computer = null)
        {
            this.reporting.PerformAuditFailureActions(model, userMessage, eventID, logMessage, ex, target, authorizationResponse, user, computer);
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
        private void UpdateTargetPasswordExpiry(ITarget target, IComputer computer)
        {
            this.logger.Trace($"Target rule requires password to change after {target.ExpireAfter}");
            DateTime newDateTime = DateTime.UtcNow.Add(target.ExpireAfter);
            this.directory.SetPasswordExpiryTime(computer, newDateTime);
            this.logger.Trace($"Set expiry time for {computer.SamAccountName} to {newDateTime.ToLocalTime()}");
        }
    }
}