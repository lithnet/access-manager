using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    public class OptionsMonitorLicenseDataProvider : IMonitoredLicenseDataProvider
    {
        private readonly IOptionsMonitor<LicensingOptions> options;

        public OptionsMonitorLicenseDataProvider(IOptionsMonitor<LicensingOptions> options)
        {
            this.options = options;
            this.options.OnChange((x, y) => this.OnLicenseDataChanged?.Invoke(this, new EventArgs()));
        }

        public event EventHandler OnLicenseDataChanged;

        public string GetRawLicenseData()
        {
            try
            {
                string data = this.options.CurrentValue.Data;
                return string.IsNullOrWhiteSpace(data) ? EmbeddedResourceProvider.GetResourceString("license.dat") : data;
            }
            catch
            {
                return null;
            }
        }
    }
}
