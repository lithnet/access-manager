namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IWebhookChannelSettings : IChannelSettings
    {
        string ContentType { get; }

        string HttpMethod { get; }

        string TemplateFailure { get; }

        string TemplateSuccess { get; }

        string Url { get; }
    }
}