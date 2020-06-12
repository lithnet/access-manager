using Lithnet.Laps.Web.ActiveDirectory;
using NLog;
using System;

namespace Lithnet.Laps.Web.Authorization
{
    public class AceEvaluator : IAceEvaluator
    {
        private readonly IDirectory directory;
        private readonly ILogger logger;

        public AceEvaluator(IDirectory directory, ILogger logger)
        {
            this.directory = directory;
            this.logger = logger;
        }

        public bool IsMatchingAce(IAce ace, ISecurityPrincipal user)
        {
            ISecurityPrincipal trustee;
            try
            {
                trustee = this.directory.GetPrincipal(ace.Sid ?? ace.Name);

                this.logger.Trace($"Ace trustee {ace.Sid ?? ace.Name} found in directory as {trustee.DistinguishedName}");

                return this.directory.IsSidInPrincipalToken(trustee.Sid, user, trustee.Sid.AccountDomainSid);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "An error occurred matching the ACE");

                if (ace.Type == AceType.Deny)
                {
                    throw;
                }
            }

            return false;
        }
    }
}
