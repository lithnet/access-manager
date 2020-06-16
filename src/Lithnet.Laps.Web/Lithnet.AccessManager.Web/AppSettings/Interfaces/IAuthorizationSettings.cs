namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IAuthorizationSettings
    {
        string JsonAuthorizationFile { get; }

        bool JsonProviderEnabled { get; }

        bool PowershellProviderEnabled { get; }

        string PowershellScriptFile { get; }

        int PowershellScriptTimeout { get; }
    }
}