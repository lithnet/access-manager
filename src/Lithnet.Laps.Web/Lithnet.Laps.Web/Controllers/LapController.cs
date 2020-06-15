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
using System.Collections.Generic;
using System.Linq;

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
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly IPasswordProvider passwordProvider;
        private readonly IJitProvider jitProvider;

        public LapController(IAuthorizationService authorizationService, ILogger logger, IDirectory directory,
            IAuditEventProcessor reporting, IRateLimiter rateLimiter, IUserInterfaceSettings userInterfaceSettings, IAuthenticationProvider authenticationProvider, IPasswordProvider passwordProvider, IJitProvider jitProvider)
        {
            this.authorizationService = authorizationService;
            this.logger = logger;
            this.directory = directory;
            this.reporting = reporting;
            this.rateLimiter = rateLimiter;
            this.userInterfaceSettings = userInterfaceSettings;
            this.authenticationProvider = authenticationProvider;
            this.passwordProvider = passwordProvider;
            this.jitProvider = jitProvider;
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
                    logger.LogEventError(EventIDs.ReasonRequired, string.Format(LogMessages.ReasonRequired, user.MsDsPrincipalName));

                    model.FailureReason = UIMessages.ReasonRequired;
                    return this.View("Get", model);
                }

                this.ThrowOnInvalidRequestType(model.RequestType);

                try
                {
                    user = this.authenticationProvider.GetLoggedInUser() ?? throw new ObjectNotFoundException();
                }
                catch (ObjectNotFoundException ex)
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

                this.logger.LogEventSuccess(EventIDs.UserRequestedPassword, string.Format(LogMessages.UserHasRequestedPassword, user.MsDsPrincipalName, model.ComputerName));

                IComputer computer;

                try
                {
                    computer = this.directory.GetComputer(model.ComputerName) ?? throw new ObjectNotFoundException();
                }
                catch (AmbiguousNameException ex)
                {
                    this.logger.LogEventError(EventIDs.ComputerNameAmbiguous, string.Format(LogMessages.ComputerNameAmbiguous, user.MsDsPrincipalName, model.ComputerName), ex);

                    model.FailureReason = UIMessages.ComputerNameAmbiguous;
                    return this.View("Get", model);
                }
                catch (ObjectNotFoundException ex)
                {
                    this.logger.LogEventError(EventIDs.ComputerNotFound, string.Format(LogMessages.ComputerNotFoundInDirectory, user.MsDsPrincipalName, model.ComputerName), ex);

                    model.FailureReason = UIMessages.ComputerNotFoundInDirectory;
                    return this.View("Get", model);
                }

                // Do authorization check first.

                AuthorizationResponse authResponse = this.authorizationService.GetAuthorizationResponse(user, computer, model.RequestType);

                if (!authResponse.IsAuthorized())
                {
                    this.AuditAuthZFailure(model, authResponse, user, computer);
                    model.FailureReason = UIMessages.NotAuthorized;
                    return this.View("Get", model);
                }

                // Do actual work only if authorized.
                if (authResponse.EvaluatedAccess == AccessMask.Laps)
                {
                    return this.GetLapsPassword(model, user, computer, (LapsAuthorizationResponse)authResponse);
                }
                else
                {
                    return this.GrantJitAccess(model, user, computer, (JitAuthorizationResponse)authResponse);
                }
            }
            catch (AuditLogFailureException ex)
            {
                this.logger.LogEventError(EventIDs.AuthZFailedAuditError, string.Format(LogMessages.AuthZFailedAuditError, user?.MsDsPrincipalName ?? LogMessages.UnknownComputerPlaceholder, model.ComputerName), ex);

                model.FailureReason = UIMessages.AccessDenied;
                return this.View("Get", model);
            }
            catch (Exception ex)
            {
                this.logger.LogEventError(EventIDs.UnexpectedError, string.Format(LogMessages.UnhandledError, model.ComputerName, user?.MsDsPrincipalName ?? LogMessages.UnknownComputerPlaceholder), ex);

                model.FailureReason = UIMessages.UnexpectedError;
                return this.View("Get", model);
            }
        }

        private void ThrowOnInvalidRequestType(AccessMask requestType)
        {
            if ((!userInterfaceSettings.AllowJit && requestType.HasFlag(AccessMask.Jit)) ||
                !userInterfaceSettings.AllowLaps && requestType.HasFlag(AccessMask.Laps))
            {
                throw new ArgumentException("The user requested an access type that was not allowed by the application configuration");
            }

            if (requestType != AccessMask.Jit && requestType != AccessMask.Laps)
            {
                throw new ArgumentException($"The user requested an access type that was not supported: {requestType}");
            }
        }

        private IActionResult GrantJitAccess(LapRequestModel model, IUser user, IComputer computer, JitAuthorizationResponse authResponse)
        {
            this.jitProvider.GrantJitAccess(computer, this.directory.GetGroup(authResponse.AuthorizingGroup), user, authResponse.ExpireAfter);

            DateTime expiryDate = DateTime.Now.Add(authResponse.ExpireAfter);

            this.reporting.GenerateAuditEvent(new AuditableAction
            {
                AuthzResponse = authResponse,
                RequestModel = model,
                IsSuccess = true,
                User = user,
                Computer = computer,
                EventID = EventIDs.JitGranted,
                ComputerExpiryDate = expiryDate.ToString()
            });

            var jitDetails = new JitDetailsModel(computer.MsDsPrincipalName, user.MsDsPrincipalName, expiryDate);

            return this.View("Jit", jitDetails);
        }

        private IActionResult GetLapsPassword(LapRequestModel model, IUser user, IComputer computer, LapsAuthorizationResponse authResponse)
        {
            IList<PasswordEntry> entries;
            
            try
            {
                entries = this.passwordProvider.GetPasswordEntries(computer, authResponse.ExpireAfter);
            }
            catch (NoPasswordException)
            {
                this.logger.LogEventError(EventIDs.LapsPasswordNotPresent, string.Format(LogMessages.NoLapsPassword, computer.MsDsPrincipalName, user.MsDsPrincipalName));

                model.FailureReason = UIMessages.NoLapsPassword;
                return this.View("Get", model);
            }

            this.reporting.GenerateAuditEvent(new AuditableAction
            {
                AuthzResponse = authResponse,
                RequestModel = model,
                IsSuccess = true,
                User = user,
                Computer = computer,
                EventID = EventIDs.PasswordAccessed,
                ComputerExpiryDate = entries.First().ExpiryDate?.ToString(CultureInfo.CurrentUICulture)
            });

            return this.View("Show", new LapEntryModel(computer, entries));
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
                action.Message = string.Format(LogMessages.RateLimitExceededUser, user.MsDsPrincipalName, rateLimitResult.IPAddress, rateLimitResult.Threshold, rateLimitResult.Duration);
            }
            else
            {
                action.EventID = EventIDs.RateLimitExceededIP;
                action.Message = string.Format(LogMessages.RateLimitExceededIP, user.MsDsPrincipalName, rateLimitResult.IPAddress, rateLimitResult.Threshold, rateLimitResult.Duration);
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
                Message = string.Format(LogMessages.AuthorizationFailed, user.MsDsPrincipalName, model.ComputerName)
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
    }
}