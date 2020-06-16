namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IAuthenticationSettings
    {
        string Mode { get; }

        bool ShowPii { get; }
    }
}