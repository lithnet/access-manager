using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class RateLimitsViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly RateLimitOptions model;

        public RateLimitsViewModel(RateLimitOptions model)
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
        }

        public bool PerIPEnabled { get => this.model.PerIP.Enabled; set => this.model.PerIP.Enabled = value; }

        public int PerIPRequestsPerMinute { get => this.model.PerIP.RequestsPerMinute; set => this.model.PerIP.RequestsPerMinute = value; }
        
        public int PerIPRequestsPerHour { get => this.model.PerIP.RequestsPerHour; set => this.model.PerIP.RequestsPerHour = value; }

        public int PerIPRequestsPerDay { get => this.model.PerIP.RequestsPerDay; set => this.model.PerIP.RequestsPerDay = value; }


        public bool PerUserEnabled { get => this.model.PerUser.Enabled; set => this.model.PerUser.Enabled = value; }

        public int PerUserRequestsPerMinute { get => this.model.PerUser.RequestsPerMinute; set => this.model.PerUser.RequestsPerMinute = value; }

        public int PerUserRequestsPerHour { get => this.model.PerUser.RequestsPerHour; set => this.model.PerUser.RequestsPerHour = value; }

        public int PerUserRequestsPerDay { get => this.model.PerUser.RequestsPerDay; set => this.model.PerUser.RequestsPerDay = value; }
        
        public string DisplayName { get; set; } = "Rate limits";
    }
}
