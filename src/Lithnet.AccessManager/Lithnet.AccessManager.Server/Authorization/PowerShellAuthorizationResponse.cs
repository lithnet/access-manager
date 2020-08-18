namespace Lithnet.AccessManager.Server.Authorization
{
    public class PowerShellAuthorizationResponse
    {
        public bool IsLocalAdminPasswordDenied { get; set; }

        public bool IsLocalAdminPasswordAllowed { get; set; }

        public bool IsLocalAdminPasswordHistoryAllowed { get; set; }

        public bool IsLocalAdminPasswordHistoryDenied { get; set; }

        public bool IsJitAllowed { get; set; }

        public bool IsJitDenied { get; set; }

        public bool IsBitLockerAllowed { get; set; }

        public bool IsBitLockerDenied { get; set; }
    }
}
