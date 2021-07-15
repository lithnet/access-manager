using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Licensing.Core;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    public class OptionsLicenseDataProvider : ILicenseDataProvider
    {
        private readonly LicensingOptions options;
        private readonly IRegistryProvider registryProvider;

        public OptionsLicenseDataProvider(LicensingOptions options, IRegistryProvider registryProvider)
        {
            this.options = options;
            this.registryProvider = registryProvider;
        }

        public string GetRawLicenseData()
        {
            try
            {
                string data = registryProvider.LicenseData ?? this.options.Data;
                return string.IsNullOrWhiteSpace(data) ? EmbeddedResourceProvider.GetResourceString("license.dat") : data;
            }
            catch
            {
                return null;
            }
        }

        public void LicenseDataChanged()
        {
            this.OnLicenseDataChanged?.Invoke(this, new EventArgs());
        }

        public event EventHandler OnLicenseDataChanged;
    }
}