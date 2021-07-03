namespace Lithnet.AccessManager.Agent
{
    internal class EventIDs 
    {
        public const int AgentStarted = 1000;
        public const int AgentDisabled = 1001;
        public const int LapsAgentDisabled = 1002;
        public const int LapsAgentEnabled = 1020;

        public const int RunningOnDC = 1004;
        public const int LapsUnexpectedException = 1006;
        public const int AgentUnexpectedException = 1007;

        public const int LapsAgentNotConfigured = 1008;
        public const int LapsConflict = 1009;

        public const int PasswordExpired = 1010;
        public const int SetPasswordOnLapsAttribute = 1011;
        public const int SetPasswordOnAmAttribute = 1012;
        public const int SetPassword = 1013;

        public const int PasswordExpiryCheckFailure = 1014;
        public const int PasswordChangeFailure = 1015;

        public const int LapsConflictResolved = 1017;

        public const int PasswordChangeNotRequired = 1018;


        // Errors
        public const int ServerConnectionError = 2001;
        public const int ServerCredentialsNotRecognized = 2002;
        public const int NoServerConfigured = 2003;
        public const int AadrRegistrationNotAllowed = 2004;
        public const int AmsRegistrationRejected = 2005;
        public const int AmsRegistrationMissing = 2006;
        public const int AdCertificatePrivateKeyNotAvailable = 2007;
        public const int ImpersonationFailure = 2008;
        public const int AmsRegistrationInvalidRegistrationKey = 2009;
        public const int EnableAccountFailed = 2010;

        // Warning
        public const int RegistrationNotReady = 3001;
        public const int NoSuitableAadTenantFound = 3002;

        // Informational
        public const int AmsRegistrationPending = 4001;
        public const int AmsRegistrationApproved = 4002;
        public const int AmsRegistrationStarting = 4003;
        public const int RegisteredSecondaryCredentials = 4004;


    }
}
