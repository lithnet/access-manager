namespace Lithnet.AccessManager
{
    public class EventIDs
    {
        // 2000 range - informational operations
        public const int UserRequestedAccessToComputer = 2001;
        public const int LocalSamGroupMemberAdded = 2001;
        public const int LocalSamGroupMemberRemoved = 2002;
        public const int JitWorkerGroupCreated = 2003;
        public const int JitWorkerGroupDeleted = 2004;
        public const int JitDynamicGroupCreated = 2005;
        public const int JitDynamicGroupDeleted = 2006;
        public const int JitPamAccessGranted = 2007;
        public const int JitPamAccessRevoked = 2008;

        // 3000 range - Audit Success
        public const int UserAuthenticated = 3000;
        public const int ComputerPasswordActiveAccessGranted = 3001;
        public const int ComputerPasswordHistoryAccessGranted = 3002;
        public const int ComputerJitAccessGranted = 3003;
        public const int ComputerBitLockerAccessGranted = 3004;

        // 4000 range - User errors
        public const int SsoIdentityNotFound = 4000;
        public const int ComputerNotFoundInDirectory = 4001;
        public const int LapsPasswordNotPresent = 4002;
        public const int ComputerNameAmbiguous = 4003;
        public const int ReasonRequired = 4004;
        public const int CertificateIdentityNotFound = 4006;
        public const int CertificateValidationError = 4008;
        public const int IdentityDiscoveryError = 4009;
        public const int ComputerDiscoveryError = 4010;
        public const int NoLapsPasswordHistory = 4011;
        public const int AuthZFailedNoReaderPrincipalMatch = 4012;
        public const int AuthZFailedNoTargetMatch = 4013;
        public const int AuthZFailed = 4014;
        public const int AuthZExplicitlyDenied = 4015;
        public const int AuthZFailedAuditError = 4016;
        public const int RateLimitExceededIP = 4017;
        public const int RateLimitExceededUser = 4018;
        public const int BitLockerKeysNotPresent = 4019;

        // 5000 range - Server errors
        public const int LocalSamGroupMemberAddFailed = 5001;
        public const int LocalSamGroupMemberRemoveFailed = 5002;
        public const int CertProviderInvalidUnsupportedUriScheme = 5003;
        public const int UnexpectedError = 5004;
        public const int ErrorLoadingTemplateResource = 5005;
        public const int NotificationChannelError = 5006;
        public const int BackgroundTaskUnhandledError = 5007;
        public const int TargetRuleProcessingError = 5008;
        public const int JitRollbackInProgress = 5009;
        public const int JitRollbackFailed = 5010;
        public const int JitError = 5011;
        public const int PreAuthZError = 5012;
        public const int LapsPasswordHistoryError = 5013;
        public const int LapsPasswordError = 5014;
        public const int AuthZError = 5015;
        public const int AppNotConfigured = 5016;
        public const int CertificateTrustChainParsingIssue = 5017;
        public const int TargetDirectoryLookupError = 5018;
        public const int AuthZContextCreateError = 5019;
        public const int AuthZContextFallback = 5020;
        public const int AuthZContextServerCantConnect = 5021;
        public const int PowerShellSDGeneratorInvalidResponse = 5022;
        public const int DNParseError = 5023;
        public const int JitWorkerUnexpectedError = 5024;
        public const int JitWorkerGroupCreateError = 5025;
        public const int JitWorkerGroupDeleteError = 5026;
        public const int JitWorkerUsnFallback = 5027;
        public const int JitDynamicGroupInvalidDomain = 5028;
        public const int ExternalAuthNProviderError = 5029;
        public const int AuthNResponseProcessingError = 5030;
        public const int AuthNDirectoryLookupError = 5031;
        public const int ExternalAuthNAccessDenied = 5032;
        public const int CertificateAuthNAccessDenied = 5033;
        public const int CertificateAuthNError = 5034;
        public const int BitLockerKeyAccessError = 5035;
        public const int ResourceReadError = 5036;
        public const int CertificateSynchronizationImportError = 5037;
        public const int CertificateSynchronizationExportError = 5038;
        public const int CertificateSynchronizationExportWarningNoPrivateKey = 5039;
        public const int DbUpgradeError = 5040;
        public const int DbUpgradeWarning = 5041;
        public const int DbUpgradeInfo = 5042;
        public const int DbUpgradeRequired = 5043;
        public const int DbNotFound = 5044;
        public const int DbCreated = 5045;


        // 6000 range - Reserved 

        // 8000 range - UI errors
        public const int UIGroupMembershipLookupError = 8000;
        public const int UISchemaLookupError = 8001;
        public const int UIGenericError = 8002;
        public const int UIConfigurationSaveError = 8003;
        public const int UIConfigurationRollbackError = 8004;
        public const int UIInitializationError = 8005;
        public const int UIGenericWarning = 8006;
    }
}
