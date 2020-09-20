using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.PowerShell
{
    [Cmdlet(VerbsData.Export, "LocalAdministrators")]
    public class ExportLocalAdministrators : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public string BaseOU { get; set; }

        [Parameter(Mandatory = true)]
        public string OutputFile { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeUnresolvedPrincipals { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeLocalPrincipals { get; set; }

        [Parameter(Mandatory = false)]
        public string JitGroupTemplate { get; set; }

        [Parameter(Mandatory = false)]
        public string CsvFile { get; set; }

        [Parameter(Mandatory = false)]
        public string[] PrincipalFilters { get; set; }

        [Parameter(Mandatory = false)]
        public string[] ComputerFilters { get; set; }

        protected override void ProcessRecord()
        {
            var provider = this.GetAuthorizationImportRuleProvider();

            List<Regex> computerFilters = new List<Regex>();
            List<Regex> principalFilters = new List<Regex>();

            if (PrincipalFilters != null)
            {
                foreach (string item in PrincipalFilters)
                {
                    principalFilters.Add(new Regex(item, RegexOptions.IgnoreCase));
                }
            }

            if (ComputerFilters != null)
            {
                foreach (string item in ComputerFilters)
                {
                    computerFilters.Add(new Regex(item, RegexOptions.IgnoreCase));
                }
            }

            if (CsvFile != null)
            {
                var entry = provider.BuildPrincipalMap(this.BaseOU, this.CsvFile, principalFilters, computerFilters, !this.IncludeLocalPrincipals, !this.IncludeUnresolvedPrincipals, CancellationToken.None);
                provider.WriteReport(entry, this.OutputFile);
            }
            else
            {
                var entry = provider.BuildPrincipalMap(this.BaseOU, principalFilters, computerFilters, !this.IncludeLocalPrincipals, !this.IncludeUnresolvedPrincipals, CancellationToken.None);
                provider.WriteReport(entry, this.OutputFile);
            }
        }

        private AuthorizationRuleImportProvider GetAuthorizationImportRuleProvider()
        {
            var logFactory = LoggerFactory.Create(options =>
            {
            });

            ILocalSam localSam = new LocalSam(logFactory.CreateLogger<LocalSam>());
            IDiscoveryServices discoveryServices = new DiscoveryServices(logFactory.CreateLogger<DiscoveryServices>());
            IDirectory directory = new ActiveDirectory(discoveryServices);
            IComputerPrincipalProviderRpc rpcProvider = new ComputerPrincipalProviderRpc(localSam, directory, logFactory.CreateLogger<ComputerPrincipalProviderRpc>());
            IComputerPrincipalProviderCsv csvProvider = new ComputerPrincipalProviderCsv(directory, logFactory.CreateLogger<ComputerPrincipalProviderCsv>());

            return new AuthorizationRuleImportProvider(logFactory.CreateLogger<AuthorizationRuleImportProvider>(), directory, csvProvider, rpcProvider);
        }
    }
}
