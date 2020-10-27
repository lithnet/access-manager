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
    public class OptionsLicenseDataProvider : ILicenseDataProvider
    {
        private readonly LicensingOptions options;

        public OptionsLicenseDataProvider(LicensingOptions options)
        {
            this.options = options;
        }

        public string GetRawLicenseData()
        {
            try
            {
                string data = this.options.Data;
                return string.IsNullOrWhiteSpace(data) ? EmbeddedResourceProvider.GetResourceString("license.dat") : data;
            }
            catch
            {
                return null;
            }
        }
    }
}
