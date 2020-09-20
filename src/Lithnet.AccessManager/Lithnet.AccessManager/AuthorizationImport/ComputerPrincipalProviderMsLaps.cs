using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Vanara;

namespace Lithnet.AccessManager
{
    public class ComputerPrincipalProviderMsLaps : IComputerPrincipalProviderMsLaps
    {
        private readonly ILogger<ComputerPrincipalProviderRpc> logger;
        private readonly IDiscoveryServices discoveryServices;

        public ComputerPrincipalProviderMsLaps(IDiscoveryServices discoveryServices, ILogger<ComputerPrincipalProviderRpc> logger)
        {
            this.logger = logger;
            this.discoveryServices = discoveryServices;
            this.ComputerPropertiesToGet = new List<string>() { "objectSid", "ntSecurityDescriptor", "msDS-PrincipalName" };
        }

        public List<SecurityIdentifier> GetPrincipalsForComputer(SearchResult computer, bool filterLocalAccounts)
        {
            byte[] sd = computer.GetPropertyBytes("ntSecurityDescriptor");

            if (sd == null)
            {
                throw new InvalidOperationException($"Security descriptor for computer {computer.GetPropertyString("msDS-PrincipalName")} was empty");
            }

            var sec = new ActiveDirectorySecurity();
            sec.SetSecurityDescriptorBinaryForm(sd);

            string domain = this.discoveryServices.GetDomainNameDns(computer.GetPropertySid("objectSid"));
            Guid mslapsGuid = this.discoveryServices.GetSchemaAttributeGuid(domain, "ms-Mcs-AdmPwd") ?? throw new ObjectNotFoundException("The ms-Mcs-AdmPwd attribute was not found in the schema");

            List<SecurityIdentifier> results = new List<SecurityIdentifier>();

            foreach (ActiveDirectoryAccessRule accessRule in sec.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                var sid = accessRule.IdentityReference as SecurityIdentifier;
                if (sid == null)
                {
                    continue;
                }

                if (sid.IsWellKnown(WellKnownSidType.LocalSystemSid))
                {
                    continue;
                }

                if (accessRule.ActiveDirectoryRights == ActiveDirectoryRights.GenericAll)
                {
                    results.Add(sid);
                    continue;
                }

                if (accessRule.ObjectType == mslapsGuid)
                {
                    if (accessRule.AccessControlType != AccessControlType.Allow)
                    {
                        continue;
                    }

                    if (!accessRule.ActiveDirectoryRights.HasFlag((ActiveDirectoryRights)0x110))
                    {
                        continue;
                    }

                    results.Add(sid);
                }
            }


            return results;
        }

        public List<string> ComputerPropertiesToGet { get; }
    }
}
