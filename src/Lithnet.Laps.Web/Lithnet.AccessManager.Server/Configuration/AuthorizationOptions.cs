namespace Lithnet.AccessManager.Configuration
{
    public class AuthorizationOptions
    {
        public PowershellAuthorizationProviderOptions PowershellProvider { get; set; }

        public JsonFileTargetsProviderOptions JsonProvider { get; set; }
    }
}