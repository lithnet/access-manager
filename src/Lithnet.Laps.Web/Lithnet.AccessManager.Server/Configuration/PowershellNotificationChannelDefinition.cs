namespace Lithnet.AccessManager.Server.Configuration
{
    public class PowershellNotificationChannelDefinition : NotificationChannelDefinition
    {
        public string Script { get; set; }

        public int TimeOut { get; set; } = 10;
    }
}
