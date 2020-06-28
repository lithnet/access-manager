namespace Lithnet.AccessManager.Configuration
{
    public class PowershellNotificationChannelDefinition : NotificationChannelDefinition
    {
        public string Script { get; set; }

        public int TimeOut { get; set; } = 10;
    }
}
