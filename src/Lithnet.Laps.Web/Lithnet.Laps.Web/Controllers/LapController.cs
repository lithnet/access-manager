using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Models;
using NLog;

namespace Lithnet.Laps.Web.Controllers
{
    [Authorize]
    [Localizable(true)]
    public class LapController : Controller
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public ActionResult Get()
        {
            return this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Get(LapRequestModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View();
            }

            UserPrincipal user = null;

            try
            {
                model.FailureReason = null;

                try
                {
                    user = this.GetCurrentUser();

                    if (user == null)
                    {
                        throw new NoMatchingPrincipalException();
                    }
                }
                catch (NoMatchingPrincipalException ex)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.SsoIdentityNotFound, EventIDs.SsoIdentityNotFound, null, ex);
                }

                if (RateLimiter.IsRateLimitExceeded(model, user, this.Request))
                {
                    return this.View("RateLimitExceeded");
                }

                Reporting.LogSuccessEvent(EventIDs.UserRequestedPassword, string.Format(LogMessages.UserHasRequestedPassword, user.SamAccountName, model.ComputerName));

                ComputerPrincipal computer = Directory.GetComputerPrincipal(model.ComputerName);

                if (computer == null)
                {
                    return this.LogAndReturnErrorResponse(model, UIMessages.ComputerNotFoundInDirectory, EventIDs.ComputerNotFound, string.Format(LogMessages.ComputerNotFoundInDirectory, user.SamAccountName, model.ComputerName));
                }

                TargetElement target = this.GetMatchingTargetOrNull(computer);
                if (target == null)
                {
                    return this.AuditAndReturnErrorResponse(
                        model: model,
                        userMessage: UIMessages.NotAuthorized,
                        eventID: EventIDs.AuthZFailedNoTargetMatch,
                        logMessage: string.Format(LogMessages.NoTargetsExist, user.SamAccountName, computer.SamAccountName),
                        user: user,
                        computer: computer);
                }

                foreach (ReaderElement reader in target.Readers.OfType<ReaderElement>())
                {
                    if (this.IsReaderAuthorized(reader, user))
                    {
                        logger.Trace($"User {user.SamAccountName} matches reader principal {reader.Principal} is authorized to read passwords from target {target.Name}");

                        SearchResult searchResult = Directory.GetDirectoryEntry(computer, Directory.AttrSamAccountName, Directory.AttrMsMcsAdmPwd, Directory.AttrMsMcsAdmPwdExpirationTime);

                        if (!searchResult.Properties.Contains(Directory.AttrMsMcsAdmPwd))
                        {
                            return this.LogAndReturnErrorResponse(model, UIMessages.NoLapsPassword, EventIDs.LapsPasswordNotPresent, string.Format(LogMessages.NoLapsPassword, computer.SamAccountName, user.SamAccountName));
                        }

                        if (target.ExpireAfter != null)
                        {
                            LapController.UpdateTargetPasswordExpiry(target, computer);
                            searchResult = Directory.GetDirectoryEntry(computer, Directory.AttrSamAccountName, Directory.AttrMsMcsAdmPwd, Directory.AttrMsMcsAdmPwdExpirationTime);
                        }

                        Reporting.PerformAuditSuccessActions(model, target, reader, user, computer, searchResult);

                        return this.View("Show", LapController.CreateLapEntryModel(searchResult));
                    }
                }

                return this.AuditAndReturnErrorResponse(
                    model: model,
                    userMessage: UIMessages.NotAuthorized,
                    eventID: EventIDs.AuthZFailedNoReaderPrincipalMatch,
                    logMessage: string.Format(LogMessages.AuthZFailedNoReaderPrincipalMatch, user.SamAccountName, computer.SamAccountName),
                    target: target,
                    user: user,
                    computer: computer);
            }
            catch (Exception ex)
            {
                return this.LogAndReturnErrorResponse(model, UIMessages.UnexpectedError, EventIDs.UnexpectedError, string.Format(LogMessages.UnhandledError, model.ComputerName, user?.SamAccountName ?? LogMessages.UnknownComputerPlaceholder), ex);
            }
        }

        private ViewResult AuditAndReturnErrorResponse(LapRequestModel model, string userMessage, int eventID, string logMessage = null, Exception ex = null, TargetElement target = null, ReaderElement reader = null, UserPrincipal user = null, ComputerPrincipal computer = null)
        {
            Reporting.PerformAuditFailureActions(model, userMessage, eventID, logMessage, ex, target, reader, user, computer);
            model.FailureReason = userMessage;
            return this.View("Get", model);
        }

        private ViewResult LogAndReturnErrorResponse(LapRequestModel model, string userMessage, int eventID, string logMessage = null, Exception ex = null)
        {
            Reporting.LogErrorEvent(eventID, logMessage ?? userMessage, ex);
            model.FailureReason = userMessage;
            return this.View("Get", model);
        }

        private TargetElement GetMatchingTargetOrNull(ComputerPrincipal computer)
        {
            List<TargetElement> matchingTargets = new List<TargetElement>();

            foreach (TargetElement target in LapsConfigSection.Configuration.Targets.OfType<TargetElement>().OrderBy(t => t.Type == TargetType.Computer).ThenBy(t => t.Type == TargetType.Group))
            {
                if (target.Type == TargetType.Container)
                {
                    if (Directory.IsPrincipalInOu(computer, target.Name))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target OU {target.Name}");
                        matchingTargets.Add(target);
                    }

                    continue;
                }
                else if (target.Type == TargetType.Computer)
                {
                    ComputerPrincipal p = Directory.GetComputerPrincipal(target.Name);

                    if (p == null)
                    {
                        logger.Trace($"Target computer {target.Name} was not found in the directory");
                        continue;
                    }

                    if (p.Equals(computer))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target computer {target.Name}");
                        return target;
                    }
                }
                else
                {
                    GroupPrincipal g = Directory.GetGroupPrincipal(target.Name);

                    if (g == null)
                    {
                        logger.Trace($"Target group {target.Name} was not found in the directory");
                        continue;
                    }

                    if (Directory.IsPrincipalInGroup(computer, g))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target group {target.Name}");
                        matchingTargets.Add(target);
                    }
                }
            }

            return matchingTargets.OrderBy(t => t.Type == TargetType.Computer).ThenBy(t => t.Type == TargetType.Group).FirstOrDefault();
        }

        private bool IsReaderAuthorized(ReaderElement reader, UserPrincipal currentUser)
        {
            Principal readerPrincipal = Directory.GetPrincipal(reader.Principal);

            if (currentUser.Equals(readerPrincipal))
            {
                return true;
            }

            if (readerPrincipal is GroupPrincipal group)
            {
                if (Directory.IsPrincipalInGroup(currentUser, group))
                {
                    return true;
                }
            }

            return false;
        }

        private UserPrincipal GetCurrentUser()
        {
            ClaimsPrincipal p = (ClaimsPrincipal)this.User;

            string sid = p.FindFirst(ClaimTypes.PrimarySid)?.Value;

            UserPrincipal u = null;

            if (sid != null)
            {
                u = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain), IdentityType.Sid, sid);
            }

            return u ?? throw new NoMatchingPrincipalException(string.Format(LogMessages.UserNotFoundInDirectory, this.User.Identity.Name));
        }

        private static LapEntryModel CreateLapEntryModel(SearchResult result)
        {
            return new LapEntryModel
            {
                ComputerName = result.Properties[Directory.AttrSamAccountName][0].ToString(),
                Password = result.GetPropertyString(Directory.AttrMsMcsAdmPwd) ?? UIMessages.NoLapsPasswordPlaceholder,
                ValidUntil = result.GetPropertyDateTimeFromLong(Directory.AttrMsMcsAdmPwdExpirationTime)
            };
        }

        [Localizable(false)]
        private static void UpdateTargetPasswordExpiry(TargetElement target, ComputerPrincipal computer)
        {
            TimeSpan t = TimeSpan.Parse(target.ExpireAfter);
            LapController.logger.Trace($"Target rule requires password to change after {t}");
            DateTime newDateTime = DateTime.UtcNow.Add(t);
            LapController.SetExpiryTime(new DirectoryEntry($"LDAP://{computer.DistinguishedName}"), newDateTime);
            LapController.logger.Trace($"Set expiry time for {computer.SamAccountName} to {newDateTime.ToLocalTime()}");
        }

        private static void SetExpiryTime(DirectoryEntry d, DateTime newDateTime)
        {
            d.Properties[Directory.AttrMsMcsAdmPwdExpirationTime].Value = newDateTime.ToFileTimeUtc().ToString();
            d.CommitChanges();
        }
    }
}