using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public class ComputerPrincipalProviderRpc : IComputerPrincipalProviderRpc
    {
        private readonly ILocalSam localSam;
        private readonly ILogger<ComputerPrincipalProviderRpc> logger;
        private readonly IDirectory directory;

        public ComputerPrincipalProviderRpc(ILocalSam localSam, IDirectory directory, ILogger<ComputerPrincipalProviderRpc> logger)
        {
            this.localSam = localSam;
            this.logger = logger;
            this.directory = directory;
            this.ComputerPropertiesToGet = new List<string>() {"dnsHostName", "samAccountName"};
        }

        public List<SecurityIdentifier> GetPrincipalsForComputer(SearchResult computer, bool filterLocalAccounts)
        {
            SecurityIdentifier localMachineSid = null;
            string computerDnsName = computer.GetPropertyString("dnsHostName") ?? computer.GetPropertyString("samAccountName").TrimEnd('$');

            List<SecurityIdentifier> results = new List<SecurityIdentifier>();

            try
            {
                if (filterLocalAccounts)
                {
                    localMachineSid = localSam.GetLocalMachineAuthoritySid(computerDnsName);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Unable to connect to get SID from remote computer {computer}", computerDnsName);
            }

            IList<SecurityIdentifier> members = this.localSam.GetLocalGroupMembers(computerDnsName, this.localSam.GetBuiltInAdministratorsGroupNameOrDefault(computerDnsName));

            foreach (var member in members)
            {
                if (filterLocalAccounts)
                {
                    if (localMachineSid != null)
                    {
                        if (member.IsEqualDomainSid(localMachineSid))
                        {
                            continue;
                        }
                    }
                }

                results.Add(member);
            }

            return results;
        }

        public List<string> ComputerPropertiesToGet { get; }
    }
}
