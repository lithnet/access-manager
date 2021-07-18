using System;
using System.Collections.Generic;
using System.Text;
using Lithnet.AccessManager.Enterprise;

namespace Lithnet.AccessManager.Server.UI
{
    public class EnterpriseEditionBannerModel
    {
        public EnterpriseEditionBannerModel()
        {
        }

        public string Link { get; set; }

        public string FeatureName { get; set; }

        public LicensedFeatures RequiredFeature { get; set; }
    }
}
