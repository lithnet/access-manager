namespace Lithnet.Laps.Web.AppSettings
{
    public interface IAuthenticationSettings
    {
        string Mode { get; }

        bool ShowPii { get; }
    }
}