namespace Lithnet.AccessManager.Configuration
{
    public class PowershellAuthorizationProviderOptions
    {
        public string ScriptFile { get; set; }

        public bool Enabled { get; set; }

        public int ScriptTimeout { get; set; } = 30;
    }
}
