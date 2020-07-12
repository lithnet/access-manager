namespace Lithnet.AccessManager.Server.Authorization
{
    public class PowerShellAuthorizationResponse
    {
        public bool IsDenied { get; set; }

        public bool IsAllowed { get; set; }
    }
}
