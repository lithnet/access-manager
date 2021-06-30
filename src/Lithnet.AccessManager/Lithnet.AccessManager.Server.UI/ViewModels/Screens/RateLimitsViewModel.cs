using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class RateLimitsViewModel : Screen, IHelpLink
    {
        private readonly RateLimitOptions model;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public RateLimitsViewModel(RateLimitOptions model, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.model = model;
            this.DisplayName = "Rate limits";

            if (this.model.PerUser == null)
            {
                this.model.PerUser = new RateLimitThresholds();
            }

            if (this.model.PerIP == null)
            {
                this.model.PerIP = new RateLimitThresholds();
            }

            eventPublisher.Register(this);
        }

        [NotifyModelChangedProperty]
        public bool PerIPEnabled { get => this.model.PerIP.Enabled; set => this.model.PerIP.Enabled = value; }

        [NotifyModelChangedProperty]
        public int PerIPRequestsPerMinute { get => this.model.PerIP.RequestsPerMinute; set => this.model.PerIP.RequestsPerMinute = value; }
        
        [NotifyModelChangedProperty]
        public int PerIPRequestsPerHour { get => this.model.PerIP.RequestsPerHour; set => this.model.PerIP.RequestsPerHour = value; }

        [NotifyModelChangedProperty]
        public int PerIPRequestsPerDay { get => this.model.PerIP.RequestsPerDay; set => this.model.PerIP.RequestsPerDay = value; }


        [NotifyModelChangedProperty]
        public bool PerUserEnabled { get => this.model.PerUser.Enabled; set => this.model.PerUser.Enabled = value; }

        [NotifyModelChangedProperty]
        public int PerUserRequestsPerMinute { get => this.model.PerUser.RequestsPerMinute; set => this.model.PerUser.RequestsPerMinute = value; }

        [NotifyModelChangedProperty]
        public int PerUserRequestsPerHour { get => this.model.PerUser.RequestsPerHour; set => this.model.PerUser.RequestsPerHour = value; }

        [NotifyModelChangedProperty]
        public int PerUserRequestsPerDay { get => this.model.PerUser.RequestsPerDay; set => this.model.PerUser.RequestsPerDay = value; }

        public string HelpLink => Constants.HelpLinkPageRateLimits;
        
        public PackIconMaterialKind Icon => PackIconMaterialKind.Speedometer;
        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
