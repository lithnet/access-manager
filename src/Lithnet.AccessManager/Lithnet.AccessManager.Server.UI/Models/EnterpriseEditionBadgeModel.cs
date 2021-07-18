using System;
using System.Collections.Generic;
using System.Text;
using Lithnet.AccessManager.Enterprise;

namespace Lithnet.AccessManager.Server.UI
{
    public class EnterpriseEditionBadgeModel
    {
        public EnterpriseEditionBadgeModel()
        {
        }

        public string Link { get; set; }

        public string ToolTipText { get; set; }

        public bool ShowText { get; set; }

        public LicensedFeatures RequiredFeature { get; set; }
    }
}
