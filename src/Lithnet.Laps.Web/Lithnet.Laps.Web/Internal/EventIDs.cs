using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web
{
    internal static class EventIDs
    {
        public const int PasswordAccessed = 200;
        public const int UserAuthenticated = 201;
        public const int UserRequestedPassword = 202;

        public const int SsoIdentityNotFound = 400;
        public const int ComputerNotFound = 401;
        public const int LapsPasswordNotPresent = 402;

        public const int AuthZFailedNoReaderPrincipalMatch = 500;
        public const int AuthZFailedNoTargetMatch = 501;
        public const int RateLimitExceededIP = 502;
        public const int RateLimitExceededUser = 503;
        
        public const int UnexpectedError = 600;
        public const int AuditErrorCannotSendSuccessEmail = 601;
        public const int AuditErrorCannotSendFailureEmail = 602;
        public const int ErrorLoadingTemplateResource = 603;

        public const int OidcAuthZCodeError = 700;
        public const int OidcAuthNError = 701;
        public const int WsFedAuthNError = 702;
    }
}