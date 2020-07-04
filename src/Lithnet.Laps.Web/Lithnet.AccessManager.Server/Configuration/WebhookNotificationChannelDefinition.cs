namespace Lithnet.AccessManager.Configuration
{
    public class WebhookNotificationChannelDefinition : NotificationChannelDefinition
    {
        public string TemplateSuccess { get; set; }

        public string TemplateFailure { get; set; }

        public string Url { get; set; }

        public string HttpMethod { get; set; } = "POST";

        public string ContentType { get; set; } = "application/json";
    }
}
