using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class RateLimitsViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly RateLimitOptions model;

        public RateLimitsViewModel(RateLimitOptions model, INotifiableEventPublisher eventPublisher)
        {
            this.model = model;
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

        [NotifiableProperty]
        public bool PerIPEnabled { get => this.model.PerIP.Enabled; set => this.model.PerIP.Enabled = value; }

        [NotifiableProperty]
        public int PerIPRequestsPerMinute { get => this.model.PerIP.RequestsPerMinute; set => this.model.PerIP.RequestsPerMinute = value; }
        
        [NotifiableProperty]
        public int PerIPRequestsPerHour { get => this.model.PerIP.RequestsPerHour; set => this.model.PerIP.RequestsPerHour = value; }

        [NotifiableProperty]
        public int PerIPRequestsPerDay { get => this.model.PerIP.RequestsPerDay; set => this.model.PerIP.RequestsPerDay = value; }


        [NotifiableProperty]
        public bool PerUserEnabled { get => this.model.PerUser.Enabled; set => this.model.PerUser.Enabled = value; }

        [NotifiableProperty]
        public int PerUserRequestsPerMinute { get => this.model.PerUser.RequestsPerMinute; set => this.model.PerUser.RequestsPerMinute = value; }

        [NotifiableProperty]
        public int PerUserRequestsPerHour { get => this.model.PerUser.RequestsPerHour; set => this.model.PerUser.RequestsPerHour = value; }

        [NotifiableProperty]
        public int PerUserRequestsPerDay { get => this.model.PerUser.RequestsPerDay; set => this.model.PerUser.RequestsPerDay = value; }
        
        public string DisplayName { get; set; } = "Rate limits";

        public PackIconMaterialKind Icon => PackIconMaterialKind.Speedometer;
    }
}
