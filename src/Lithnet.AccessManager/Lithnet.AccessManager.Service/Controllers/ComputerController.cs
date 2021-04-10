using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Server.Auditing;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Exceptions;
using Lithnet.AccessManager.Service.App_LocalResources;
using Lithnet.AccessManager.Service.AppSettings;
using Lithnet.AccessManager.Service.Models;
using Lithnet.Licensing.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IAuthorizationService = Lithnet.AccessManager.Server.Authorization.IAuthorizationService;

namespace Lithnet.AccessManager.Service.Controllers
{
    [Authorize(Policy = "RequireAuthorizedUser")]
    [Localizable(true)]
    public class ComputerController : Controller
    {
        private readonly IAuthenticationProvider authenticationProvider;
        private readonly IAuthorizationService authorizationService;
        private readonly IDirectory directory;
        private readonly IJitAccessProvider jitAccessProvider;
        private readonly ILogger logger;
        private readonly IPasswordProvider passwordProvider;
        private readonly IAuditEventProcessor reporting;
        private readonly UserInterfaceOptions userInterfaceSettings;
        private readonly IBitLockerRecoveryPasswordProvider bitLockerProvider;
        private readonly IAmsLicenseManager licenseManager;

        public ComputerController(IAuthorizationService authorizationService, ILogger<ComputerController> logger, IDirectory directory,
            IAuditEventProcessor reporting, IOptionsSnapshot<UserInterfaceOptions> userInterfaceSettings, IAuthenticationProvider authenticationProvider, IPasswordProvider passwordProvider, IJitAccessProvider jitAccessProvider, IBitLockerRecoveryPasswordProvider bitLockerProvider, IAmsLicenseManager licenseManager)
        {
            this.authorizationService = authorizationService;
            this.logger = logger;
            this.directory = directory;
            this.reporting = reporting;
            this.userInterfaceSettings = userInterfaceSettings.Value;
            this.authenticationProvider = authenticationProvider;
            this.passwordProvider = passwordProvider;
            this.jitAccessProvider = jitAccessProvider;
            this.bitLockerProvider = bitLockerProvider;
            this.licenseManager = licenseManager;
        }

        public IActionResult AccessRequest()
        {
            if (!TryGetUser(out _, out IActionResult actionResult))
            {
                return actionResult;
            }

            return this.View(new AccessRequestModel
            {
                ShowReason = this.userInterfaceSettings.UserSuppliedReason != AuditReasonFieldState.Hidden,
                ReasonRequired = this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AccessRequestType(AccessRequestModel model)
        {
            model.ShowReason = this.userInterfaceSettings.UserSuppliedReason != AuditReasonFieldState.Hidden;
            model.ReasonRequired = this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required;

            if (!this.ModelState.IsValid)
            {
                return this.View("AccessRequest", model);
            }

            IUser user = null;
            IComputer computer = null;
            model.FailureReason = null;

            try
            {
                if (!TryGetUser(out user, out IActionResult actionResult))
                {
                    return actionResult;
                }

                this.logger.LogInformation(EventIDs.UserRequestedAccessToComputer, string.Format(LogMessages.UserHasRequestedAccessToComputer, user.MsDsPrincipalName, model.ComputerName, model.RequestType));

                if (!ValidateRequestReason(model, user, out actionResult))
                {
                    return actionResult;
                }

                if (!TryGetComputer(model, user, out computer, out actionResult))
                {
                    return actionResult;
                }

                return GetPreAuthorizationResponse(model, user, computer);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UnexpectedError, ex, string.Format(LogMessages.UnhandledError, model.RequestType, computer?.MsDsPrincipalName, user?.MsDsPrincipalName));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.UnexpectedError
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> AccessResponse(AccessRequestModel model)
        {
            model.ShowReason = this.userInterfaceSettings.UserSuppliedReason != AuditReasonFieldState.Hidden;
            model.ReasonRequired = this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required;
            model.RequestType = model.RequestType == 0 ? AccessMask.LocalAdminPassword : model.RequestType;

            if (!this.ModelState.IsValid)
            {
                return this.View("AccessRequest", model);
            }

            IUser user = null;
            IComputer computer = null;
            model.FailureReason = null;

            try
            {
                if (!TryGetUser(out user, out IActionResult actionResult))
                {
                    return actionResult;
                }

                this.logger.LogInformation(EventIDs.UserRequestedAccessToComputer, string.Format(LogMessages.UserHasRequestedAccessToComputer, user.MsDsPrincipalName, model.ComputerName, model.RequestType));

                if (!ValidateRequestReason(model, user, out actionResult))
                {
                    return actionResult;
                }

                if (!TryGetComputer(model, user, out computer, out actionResult))
                {
                    return actionResult;
                }

                return await GetAuthorizationResponseAsync(model, user, computer);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UnexpectedError, ex, string.Format(LogMessages.UnhandledError, model.RequestType, computer?.MsDsPrincipalName, user?.MsDsPrincipalName));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.UnexpectedError
                });
            }
        }

        private void AuditAuthZFailure(AccessRequestModel model, AuthorizationResponse authorizationResponse, IUser user, IComputer computer)
        {
            AuditableAction action = new AuditableAction
            {
                AuthzResponse = authorizationResponse,
                User = user,
                Computer = computer,
                IsSuccess = false,
                RequestedComputerName = model.ComputerName,
                RequestReason = model.UserRequestReason,
                EvaluatedAccess = GetEvaluatedAccessDescription(authorizationResponse.EvaluatedAccess),
                Message = authorizationResponse.Code switch
                {
                    AuthorizationResponseCode.IpRateLimitExceeded => LogMessages.AuthZResponseIpRateLimitExceeded,
                    AuthorizationResponseCode.UserRateLimitExceeded => LogMessages.AuthZResponseUserRateLimitExceeded,
                    AuthorizationResponseCode.ExplicitlyDenied => LogMessages.AuthZResponseExplicitlyDenied,
                    AuthorizationResponseCode.NoMatchingRuleForComputer => LogMessages.AuthZResponseNoMatchingRuleForComputer,
                    AuthorizationResponseCode.NoMatchingRuleForUser => LogMessages.AuthZResponseNoMatchingRuleForUser,
                    _ => LogMessages.AuthZResponseFallback,
                },
                EventID = authorizationResponse.Code switch
                {
                    AuthorizationResponseCode.NoMatchingRuleForComputer => EventIDs.AuthZFailedNoTargetMatch,
                    AuthorizationResponseCode.NoMatchingRuleForUser => EventIDs.AuthZFailedNoReaderPrincipalMatch,
                    AuthorizationResponseCode.ExplicitlyDenied => EventIDs.AuthZExplicitlyDenied,
                    AuthorizationResponseCode.UserRateLimitExceeded => EventIDs.AuthZRateLimitExceeded,
                    AuthorizationResponseCode.IpRateLimitExceeded => EventIDs.AuthZRateLimitExceeded,
                    _ => EventIDs.AuthZFailed,
                }
            };

            this.reporting.GenerateAuditEvent(action);
        }

        private string GetEvaluatedAccessDescription(AccessMask mask)
        {
            return mask switch
            {
                AccessMask.BitLocker => UIMessages.AccessMaskBitLocker,
                AccessMask.Jit => UIMessages.AccessMaskJit,
                AccessMask.LocalAdminPassword => UIMessages.AccessMaskLocalAdminPassword,
                AccessMask.LocalAdminPasswordHistory => UIMessages.AccessMaskLocalAdminPasswordHistory,
                AccessMask.None => UIMessages.AccessMaskNone,
                _ => throw new AccessManagerException(@"The evaluated access response mask was not supported")
            };
        }

        private async Task<IActionResult> GetAuthorizationResponseAsync(AccessRequestModel model, IUser user, IComputer computer)
        {
            try
            {
                AuthorizationResponse authResponse = await this.authorizationService.GetAuthorizationResponse(user, computer, model.RequestType, this.Request.HttpContext.Connection.RemoteIpAddress);

                if (!authResponse.IsAuthorized())
                {
                    this.AuditAuthZFailure(model, authResponse, user, computer);
                    return this.View("AccessRequestError", GenerateAuthzFaiureModel(authResponse));
                }

                if (authResponse.EvaluatedAccess == AccessMask.LocalAdminPassword)
                {
                    return this.GetLapsPassword(model, user, computer, (LapsAuthorizationResponse)authResponse);
                }
                else if (authResponse.EvaluatedAccess == AccessMask.LocalAdminPasswordHistory)
                {
                    return this.GetLapsPasswordHistory(model, user, computer, (LapsHistoryAuthorizationResponse)authResponse);
                }
                else if (authResponse.EvaluatedAccess == AccessMask.Jit)
                {
                    return this.GrantJitAccess(model, user, computer, (JitAuthorizationResponse)authResponse);
                }
                else if (authResponse.EvaluatedAccess == AccessMask.BitLocker)
                {
                    return this.GetBitLockerRecoveryPasswords(model, user, computer, (BitLockerAuthorizationResponse)authResponse);
                }
                else
                {
                    throw new AccessManagerException(@"The evaluated access response mask was not supported");
                }
            }
            catch (AuditLogFailureException ex)
            {
                this.logger.LogError(EventIDs.AuthZFailedAuditError, ex, string.Format(LogMessages.AuthZFailedAuditError, user.MsDsPrincipalName, model.ComputerName, model.RequestType));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.AccessDenied,
                    Message = UIMessages.AuthZFailedAuditError
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.AuthZError, ex, string.Format(LogMessages.AuthZError, user.MsDsPrincipalName, computer.MsDsPrincipalName, model.RequestType));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.AuthZError
                });
            }
        }

        private ErrorModel GenerateAuthzFaiureModel(AuthorizationResponse authResponse)
        {
            return new ErrorModel
            {
                Heading = UIMessages.AccessDenied,
                Message = authResponse.Code switch
                {
                    AuthorizationResponseCode.IpRateLimitExceeded => UIMessages.RateLimitError,
                    AuthorizationResponseCode.UserRateLimitExceeded => UIMessages.RateLimitError,
                    _ => UIMessages.NotAuthorized
                }
            };
        }

        private IActionResult GetBitLockerRecoveryPasswords(AccessRequestModel model, IUser user, IComputer computer, BitLockerAuthorizationResponse authResponse)
        {
            try
            {
                IList<BitLockerRecoveryPassword> entries = this.bitLockerProvider.GetBitLockerRecoveryPasswords(computer);

                if (entries == null || entries.Count == 0)
                {
                    throw new NoPasswordException();
                }

                this.reporting.GenerateAuditEvent(new AuditableAction
                {
                    AuthzResponse = authResponse,
                    RequestedComputerName = model.ComputerName,
                    RequestReason = model.UserRequestReason,
                    EvaluatedAccess = GetEvaluatedAccessDescription(authResponse.EvaluatedAccess),
                    IsSuccess = true,
                    User = user,
                    Computer = computer,
                    EventID = EventIDs.ComputerBitLockerAccessGranted,
                });

                return this.View("AccessResponseBitLocker", new BitLockerRecoveryPasswordsModel()
                {
                    ComputerName = computer.MsDsPrincipalName,
                    Passwords = entries
                });
            }
            catch (NoPasswordException)
            {
                this.logger.LogError(EventIDs.BitLockerKeysNotPresent, string.Format(LogMessages.BitLockerKeysNotPresent, computer.MsDsPrincipalName, user.MsDsPrincipalName));

                model.FailureReason = UIMessages.BitLockerKeysNotPresent;

                return this.View("AccessResponseNoBitLocker", new NoPasswordModel
                {
                    Heading = UIMessages.HeadingBitLockerKeys,
                    Message = UIMessages.BitLockerKeysNotPresent,
                    ComputerName = computer.MsDsPrincipalName
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.BitLockerKeyAccessError, ex, string.Format(LogMessages.BitLockerKeyAccessError, computer.MsDsPrincipalName, user.MsDsPrincipalName));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.BitLockerKeyAccessError
                });
            }
        }

        private IActionResult GetLapsPassword(AccessRequestModel model, IUser user, IComputer computer, LapsAuthorizationResponse authResponse)
        {
            try
            {
                DateTime? newExpiry = authResponse.ExpireAfter.Ticks > 0 ? DateTime.UtcNow.Add(authResponse.ExpireAfter) : (DateTime?)null;

                PasswordEntry current = this.passwordProvider.GetCurrentPassword(computer, newExpiry, authResponse.RetrievalLocation);

                if (current == null)
                {
                    throw new NoPasswordException();
                }

                this.reporting.GenerateAuditEvent(new AuditableAction
                {
                    AuthzResponse = authResponse,
                    RequestedComputerName = model.ComputerName,
                    RequestReason = model.UserRequestReason,
                    EvaluatedAccess = GetEvaluatedAccessDescription(authResponse.EvaluatedAccess),
                    IsSuccess = true,
                    User = user,
                    Computer = computer,
                    EventID = EventIDs.ComputerPasswordActiveAccessGranted,
                    AccessExpiryDate = current.ExpiryDate?.ToLocalTime().ToString(CultureInfo.CurrentCulture)
                });

                return this.View("AccessResponseCurrentPassword", new CurrentPasswordModel()
                {
                    ComputerName = computer.MsDsPrincipalName,
                    Password = current.Password,
                    ValidUntil = current.ExpiryDate?.ToLocalTime(),
                });
            }
            catch (NoPasswordException)
            {
                this.logger.LogError(EventIDs.LapsPasswordNotPresent, string.Format(LogMessages.NoLapsPassword, computer.MsDsPrincipalName, user.MsDsPrincipalName));

                model.FailureReason = UIMessages.NoLapsPassword;

                return this.View("AccessResponseNoPasswords", new NoPasswordModel
                {
                    Heading = UIMessages.HeadingPasswordDetails,
                    Message = UIMessages.NoLapsPassword,
                    ComputerName = computer.MsDsPrincipalName
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsPasswordError, ex, string.Format(LogMessages.LapsPasswordError, computer.MsDsPrincipalName, user.MsDsPrincipalName));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.LapsPasswordError
                });
            }
        }

        private IActionResult GetLapsPasswordHistory(AccessRequestModel model, IUser user, IComputer computer, LapsHistoryAuthorizationResponse authResponse)
        {
            try
            {
                IList<PasswordEntry> history;

                try
                {
                    this.licenseManager.ThrowOnMissingFeature(LicensedFeatures.LapsHistory);
                    history = this.passwordProvider.GetPasswordHistory(computer);

                    if (history == null)
                    {
                        throw new NoPasswordException();
                    }
                }
                catch (NoPasswordException)
                {
                    this.logger.LogError(EventIDs.NoLapsPasswordHistory, string.Format(LogMessages.NoLapsPasswordHistory, computer.MsDsPrincipalName, user.MsDsPrincipalName));

                    return this.View("AccessResponseNoPasswords", new NoPasswordModel
                    {
                        Heading = UIMessages.HeadingPasswordDetails,
                        Message = UIMessages.NoLapsPasswordHistory,
                        ComputerName = computer.MsDsPrincipalName
                    });
                }

                this.reporting.GenerateAuditEvent(new AuditableAction
                {
                    AuthzResponse = authResponse,
                    RequestedComputerName = model.ComputerName,
                    RequestReason = model.UserRequestReason,
                    EvaluatedAccess = GetEvaluatedAccessDescription(authResponse.EvaluatedAccess),
                    IsSuccess = true,
                    User = user,
                    Computer = computer,
                    EventID = EventIDs.ComputerPasswordHistoryAccessGranted
                });

                return this.View("AccessResponsePasswordHistory", new PasswordHistoryModel
                {
                    ComputerName = computer.MsDsPrincipalName,
                    PasswordHistory = history
                });
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.LapsPasswordHistoryError, ex, string.Format(LogMessages.LapsPasswordHistoryError, computer.MsDsPrincipalName, user.MsDsPrincipalName));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.LapsPasswordHistoryError
                });
            }
        }

        private IActionResult GetPreAuthorizationResponse(AccessRequestModel model, IUser user, IComputer computer)
        {
            try
            {
                AuthorizationResponse authResponse = this.authorizationService.GetPreAuthorization(user, computer);

                if (!authResponse.IsAuthorized())
                {
                    this.AuditAuthZFailure(model, authResponse, user, computer);
                    return this.View("AccessRequestError", GenerateAuthzFaiureModel(authResponse));
                }

                model.AllowedRequestTypes = authResponse.EvaluatedAccess;

                if (model.AllowedRequestTypes.HasFlag(AccessMask.LocalAdminPassword))
                {
                    model.RequestType = AccessMask.LocalAdminPassword;
                }
                else if (model.AllowedRequestTypes.HasFlag(AccessMask.LocalAdminPasswordHistory))
                {
                    model.RequestType = AccessMask.LocalAdminPasswordHistory;
                }
                else
                {
                    model.RequestType = AccessMask.Jit;
                }

                model.ComputerName = computer.MsDsPrincipalName;

                return this.View("AccessRequestType", model);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.PreAuthZError, ex, string.Format(LogMessages.PreAuthZError, user.MsDsPrincipalName, computer.MsDsPrincipalName, model.RequestType));

                return this.View("AccessRequestError", new ErrorModel
                {
                    Heading = UIMessages.UnableToProcessRequest,
                    Message = UIMessages.PreAuthZError
                });
            }
        }

        private IActionResult GrantJitAccess(AccessRequestModel model, IUser user, IComputer computer, JitAuthorizationResponse authResponse)
        {
            Action undo = null;

            try
            {
                TimeSpan grantedAccessLength = this.jitAccessProvider.GrantJitAccess(this.directory.GetGroup(authResponse.AuthorizingGroup), user, computer, authResponse.AllowExtension, authResponse.ExpireAfter, out undo);

                DateTime expiryDate = DateTime.Now.Add(grantedAccessLength);

                this.reporting.GenerateAuditEvent(new AuditableAction
                {
                    AuthzResponse = authResponse,
                    RequestedComputerName = model.ComputerName,
                    RequestReason = model.UserRequestReason,
                    EvaluatedAccess = GetEvaluatedAccessDescription(authResponse.EvaluatedAccess),
                    IsSuccess = true,
                    User = user,
                    Computer = computer,
                    EventID = EventIDs.ComputerJitAccessGranted,
                    AccessExpiryDate = expiryDate.ToString(CultureInfo.CurrentCulture)
                });

                var jitDetails = new JitDetailsModel(computer.MsDsPrincipalName, user.MsDsPrincipalName, expiryDate);

                return this.View("AccessResponseJit", jitDetails);
            }
            catch (Exception ex)
            {
                if (undo != null)
                {
                    this.logger.LogWarning(EventIDs.JitRollbackInProgress, ex, LogMessages.JitRollbackInProgress, user.MsDsPrincipalName, computer.MsDsPrincipalName);

                    try
                    {
                        undo();
                    }
                    catch (Exception ex2)
                    {
                        this.logger.LogError(EventIDs.JitRollbackFailed, ex2, LogMessages.JitRollbackFailed, user.MsDsPrincipalName, computer.MsDsPrincipalName);
                    }
                }

                this.logger.LogError(EventIDs.JitError, ex, string.Format(LogMessages.JitError, computer.MsDsPrincipalName, user.MsDsPrincipalName));

                ErrorModel errorModel = new ErrorModel
                {
                    Heading = UIMessages.UnableToGrantAccess,
                    Message = UIMessages.JitError
                };

                return this.View("AccessRequestError", errorModel);
            }
        }

        private bool TryGetComputer(AccessRequestModel model, IUser user, out IComputer computer, out IActionResult failure)
        {
            computer = null;
            failure = null;

            try
            {
                computer = this.directory.GetComputer(model.ComputerName.Trim()) ?? throw new ObjectNotFoundException();
                return true;
            }
            catch (AmbiguousNameException ex)
            {
                this.logger.LogError(EventIDs.ComputerNameAmbiguous, ex, string.Format(LogMessages.ComputerNameAmbiguous, user.MsDsPrincipalName, model.ComputerName, model.RequestType));

                model.FailureReason = UIMessages.ComputerNameAmbiguous;
                failure = this.View("AccessRequest", model);
            }
            catch (ObjectNotFoundException ex)
            {
                this.logger.LogError(EventIDs.ComputerNotFoundInDirectory, ex, string.Format(LogMessages.ComputerNotFoundInDirectory, user.MsDsPrincipalName, model.ComputerName, model.RequestType));

                model.FailureReason = UIMessages.ComputerNotFoundInDirectory;
                failure = this.View("AccessRequest", model);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.ComputerDiscoveryError, ex, string.Format(LogMessages.ComputerDiscoveryError, model.ComputerName, user.MsDsPrincipalName, model.RequestType));
                model.FailureReason = UIMessages.ComputerDiscoveryError;
                failure = this.View("AccessRequest", model);
            }

            return false;
        }

        private bool TryGetUser(out IUser user, out IActionResult failure)
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

        private bool ValidateRequestReason(AccessRequestModel model, IUser user, out IActionResult actionResult)
        {
            actionResult = null;

            if (string.IsNullOrWhiteSpace(model.UserRequestReason) && this.userInterfaceSettings.UserSuppliedReason == AuditReasonFieldState.Required)
            {
                logger.LogError(EventIDs.ReasonRequired, string.Format(LogMessages.ReasonRequired, user.MsDsPrincipalName, model.RequestType, model.ComputerName));
                model.FailureReason = UIMessages.ReasonRequired;
                actionResult = this.View("AccessRequest", model);
                return false;
            }

            return true;
        }
    }
}