namespace Lithnet.AccessManager.Server.Auditing
{
    public static class EventIDs
    {
        public const int PasswordAccessed = 200;
        public const int UserAuthenticated = 201;
        public const int UserRequestedPassword = 202;
        public const int UserAuthorizedForComputer = 203;
        public const int JitGranted = 204;
        public const int PasswordHistoryAccessed = 205;

        public const int SsoIdentityNotFound = 400;
        public const int ComputerNotFound = 401;
        public const int LapsPasswordNotPresent = 402;
        public const int ComputerNameAmbiguous = 403;
        public const int ReasonRequired = 404;
        public const int CertificateMissingUpn = 405;
        public const int CertificateIdentityNotFound = 406;
        public const int CertificateValidationFailed = 407;
        public const int CertificateValidationError = 408;

        public const int AuthZFailedNoReaderPrincipalMatch = 500;
        public const int AuthZFailedNoTargetMatch = 501;
        public const int AuthZFailed = 504;
        public const int AuthZExplicitlyDenied = 505;
        public const int AuthZFailedAuditError = 506;

        public const int RateLimitExceededIP = 502;
        public const int RateLimitExceededUser = 503;

        public const int UnexpectedError = 600;
        public const int AuditErrorCannotSendSuccessEmail = 601;
        public const int AuditErrorCannotSendFailureEmail = 602;
        public const int ErrorLoadingTemplateResource = 603;
        public const int NotificationChannelError = 604;
        public const int BackgroundTaskUnhandledError = 605;
        public const int TargetRuleProcessingError = 606;

        public const int OidcAuthZCodeError = 700;
        public const int ExternalAuthNProviderError = 701;
        public const int AuthNResponseProcessingError = 702;
        public const int AuthNDirectoryLookupError = 703;
        public const int ExternalAuthNAccessDenied = 704;
        public const int CertificateAuthNAccessDenied = 705;
        public const int CertificateAuthNError = 706;

    }
}