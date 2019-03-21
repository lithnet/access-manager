namespace Lithnet.Laps.Web
{
    /// <summary>
    /// Some kind of return code.
    ///
    /// FIXME: I think EventIDs is a bad name for this class.
    /// Can't we replace this by an enum?
    /// </summary>
    internal static class EventIDs
    {
        public const int PasswordAccessed = 200;
        public const int UserAuthenticated = 201;
        public const int UserRequestedPassword = 202;
        public const int UserAuthorizedForComputer = 203;

        public const int SsoIdentityNotFound = 400;
        public const int ComputerNotFound = 401;
        public const int LapsPasswordNotPresent = 402;

        public const int AuthZFailedNoReaderPrincipalMatch = 500;
        public const int AuthZFailedNoTargetMatch = 501;
        public const int RateLimitExceededIP = 502;
        public const int RateLimitExceededUser = 503;
        public const int AuthorizationFailed = 504;
        
        public const int UnexpectedError = 600;
        public const int AuditErrorCannotSendSuccessEmail = 601;
        public const int AuditErrorCannotSendFailureEmail = 602;
        public const int ErrorLoadingTemplateResource = 603;

        public const int OidcAuthZCodeError = 700;
        public const int OwinAuthNError = 701;
    }
}