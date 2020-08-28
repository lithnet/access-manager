using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class AuthorizationContextDomainDetails
    {
        private readonly IDiscoveryServices discoveryServices;

        private IEnumerator<AuthorizationServer> searchResults;

        public AuthorizationContextDomainDetails(SecurityIdentifier sid, string domainDnsName, IDiscoveryServices discoveryServices)
        {
            this.SecurityIdentifier = sid;
            this.discoveryServices = discoveryServices;
            this.DomainDnsName = domainDnsName;
            this.IsInCurrentForest = this.GetIsInCurrentForest();
            this.IsRemoteOneWayTrust = this.GetIsOneWayTrust();
        }
        public SecurityIdentifier SecurityIdentifier { get; }

        public string DomainDnsName { get; }

        public bool IsInCurrentForest { get; }

        public bool IsRemoteOneWayTrust { get; }

        public AuthorizationServerMapping Mapping { get; set; }

        public AuthorizationServer GetServer(bool requireNew)
        {
            if (Mapping?.Servers == null || this.Mapping.Servers.Count == 0)
            {
                return new AuthorizationServer
                {
                    Name = this.discoveryServices.GetDomainController(this.DomainDnsName, Interop.DsGetDcNameFlags.DS_DIRECTORY_SERVICE_8_REQUIRED | (requireNew ? Interop.DsGetDcNameFlags.DS_FORCE_REDISCOVERY : 0)),
                    Type = AuthorizationServerType.Default
                };
            }

            searchResults ??= this.Mapping.Servers.ToList().GetEnumerator();

            if (!requireNew && searchResults.Current != null)
            {
                return searchResults.Current;
            }

            while (true)
            {
                if (!searchResults.MoveNext())
                {
                    searchResults.Reset();
                    searchResults.MoveNext();
                }

                return searchResults.Current;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AuthorizationContextDomainDetails)obj);
        }

        protected bool Equals(AuthorizationContextDomainDetails other)
        {
            return Equals(SecurityIdentifier, other.SecurityIdentifier);
        }

        public override int GetHashCode()
        {
            return (SecurityIdentifier != null ? SecurityIdentifier.GetHashCode() : 0);
        }


        private bool GetIsOneWayTrust()
        {
            var forest = Forest.GetCurrentForest();

            if (this.GetIsInCurrentForest())
            {
                return false;
            }

            var trusts = forest.GetAllTrustRelationships();

            foreach (var trust in trusts.OfType<TrustRelationshipInformation>())
            {
                if (string.Equals(this.DomainDnsName, trust.TargetName) &&
                    trust.TrustDirection == TrustDirection.Inbound)
                {
                    return true;
                }
            }

            return false;
        }

        private bool GetIsInCurrentForest()
        {
            var forest = Forest.GetCurrentForest();

            foreach (var domain in forest.Domains.OfType<Domain>())
            {
                if (string.Equals(this.DomainDnsName, domain.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
