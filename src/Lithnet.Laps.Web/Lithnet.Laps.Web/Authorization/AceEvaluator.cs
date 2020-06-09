using Lithnet.Laps.Web.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;

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

        public bool IsMatchingAce(IAce ace, IComputer computer, IUser user)
        {
            ISecurityPrincipal principal;

            try
            {
                principal = this.directory.GetPrincipal(ace.Sid ?? ace.Name);
            }
            catch (NotFoundException)
            {
                this.logger.Warn($"Could not match reader principal {ace.Sid ?? ace.Name} to a directory object");
                return false;
            }

            this.logger.Trace($"Ace principal {ace.Sid ?? ace.Name} found in directory as  {principal.DistinguishedName}");

            return this.directory.IsSidInPrincipalToken(computer.Sid.AccountDomainSid, user, principal.Sid);
        }
    }
}
