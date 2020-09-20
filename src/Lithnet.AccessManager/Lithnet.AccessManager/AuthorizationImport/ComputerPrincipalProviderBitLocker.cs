using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public class ComputerPrincipalProviderBitLocker : IComputerPrincipalProviderBitLocker
    {
        private readonly ILogger<ComputerPrincipalProviderRpc> logger;
        private readonly IDiscoveryServices discoveryServices;

        public ComputerPrincipalProviderBitLocker(IDiscoveryServices discoveryServices, ILogger<ComputerPrincipalProviderRpc> logger)
        {
            this.logger = logger;
            this.discoveryServices = discoveryServices;
            this.ComputerPropertiesToGet = new List<string> { "objectSid", "ntSecurityDescriptor" };
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
            Guid recoveryPasswordAttribute = this.discoveryServices.GetSchemaAttributeGuid(domain, "msFVE-RecoveryPassword") ?? throw new ObjectNotFoundException("The msFVE-RecoveryPassword attribute was not found in the schema");
            Guid bitLockerObject = this.discoveryServices.GetSchemaObjectGuid(domain, "msFVE-RecoveryInformation") ?? throw new ObjectNotFoundException("The msFVE-RecoveryInformation object was not found in the schema");

            List<SecurityIdentifier> results = new List<SecurityIdentifier>();

            foreach (ActiveDirectoryAccessRule accessRule in sec.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                var sid = accessRule.IdentityReference as SecurityIdentifier;
                if (sid == null)
                {
                    continue;
                }

                if (accessRule.InheritedObjectType != bitLockerObject)
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

                if (accessRule.ObjectType == recoveryPasswordAttribute)
                {
                    if (accessRule.AccessControlType != AccessControlType.Allow)
                    {
                        continue;
                    }

                    if (!accessRule.ActiveDirectoryRights.HasFlag((ActiveDirectoryRights)0x100))
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
