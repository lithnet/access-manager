namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IEmailSettings
    {
        string FromAddress { get; }
        string Host { get; }
        bool IsConfigured { get; }
        string Password { get; }
        int Port { get; }
        bool UseDefaultCredentials { get; }
        string Username { get; }
        bool UseSsl { get; }
    }
}