using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Licensing.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Vanara.Extensions.Reflection;

namespace Lithnet.AccessManager.Server
{
    public class OptionsMonitorLicenseDataProvider : ILicenseDataProvider
    {
        private readonly IOptionsMonitor<LicensingOptions> options;
        private readonly IRegistryProvider registryProvider;

        public OptionsMonitorLicenseDataProvider(IOptionsMonitor<LicensingOptions> options, IRegistryProvider registryProvider)
        {
            this.options = options;
            this.registryProvider = registryProvider;
            this.options.OnChange((x, y) => this.OnLicenseDataChanged?.Invoke(this, new EventArgs()));
        }

        public void LicenseDataChanged()
        {
            this.OnLicenseDataChanged?.Invoke(this, new EventArgs());
        }

        public event EventHandler OnLicenseDataChanged;

        public string GetRawLicenseData()
        {
            try
            {
                string data = registryProvider.LicenseData ?? this.options.CurrentValue.Data;
                return string.IsNullOrWhiteSpace(data) ? EmbeddedResourceProvider.GetResourceString("license.dat") : data;
            }
            catch
            {
                return null;
            }
        }
    }
}
