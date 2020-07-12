namespace Lithnet.AccessManager.Configuration
{
    public class AuthorizationOptions
    {
        public JsonFileTargetsProviderOptions JsonProvider { get; set; }

        public BuiltInProviderOptions BuiltInProvider { get; set; }
    }
}