using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    public class DomainTrustProvider : IDomainTrustProvider
    {
        private readonly ILogger logger;

        public DomainTrustProvider(ILogger<DomainTrustProvider> logger)
        {
            this.logger = logger;
        }

        public IList<Domain> GetDomains()
        {
            List<Domain> domains = new List<Domain>();
          
            foreach (Forest forest in this.GetForests())
            {
                domains.AddRange(this.GetDomains(forest));
            }

            return domains;
        }

        public IList<Domain> GetDomains(Forest forest)
        {
            List<Domain> domains = new List<Domain>();

            foreach (Domain domain in forest.Domains.OfType<Domain>())
            {
                domains.Add(domain);
            }

            return domains;
        }

        public IList<Forest> GetForests(bool ignoreError = true)
        {
            List<Forest> forests = new List<Forest>();
            forests.Add(Forest.GetCurrentForest());

            foreach (var trust in Forest.GetCurrentForest().GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    try
                    {
                        var forest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, trust.TargetName));
                        forests.Add(forest);
                    }
                    catch (Exception ex)
                    {
                        if (!ignoreError)
                        {
                            throw;
                        }

                        logger.LogError(EventIDs.UIGenericError, ex, $"Could not get information for forest {trust.TargetName}");
                    }
                }
            }

            return forests;
        }
    }
}
