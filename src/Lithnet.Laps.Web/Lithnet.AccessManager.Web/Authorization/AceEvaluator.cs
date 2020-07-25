using System;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class AceEvaluator : IAceEvaluator
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        public AceEvaluator(IDirectory directory, ILogger<AceEvaluator> logger)
        {
            this.directory = directory;
            this.logger = logger;
        }

        public bool IsMatchingAce(IAce ace, ISecurityPrincipal user, AccessMask requestedAccess)
        {
            ISecurityPrincipal trustee;
            try
            {
                if (!ace.Access.HasFlag(requestedAccess))
                {
                    return false;
                }

                trustee = this.directory.GetPrincipal(ace.Sid ?? ace.Trustee);

                this.logger.LogTrace($"Ace trustee {ace.Sid ?? ace.Trustee} found in directory as {trustee.DistinguishedName}");

                return this.directory.IsSidInPrincipalToken(trustee.Sid, user, trustee.Sid.AccountDomainSid);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred matching the ACE");

                if (ace.Type == AceType.Deny)
                {
                    throw;
                }
            }

            return false;
        }
    }
}
