using Lithnet.Laps.Web.ActiveDirectory;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.ActionProviders
{
    public class AdGroupJitProvider : IJitProvider
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        public AdGroupJitProvider(IDirectory directory, ILogger logger)
        {
            this.directory = directory;
            this.logger = logger;
        }

        public void GrantJitAccess(IComputer computer, IGroup group, IUser user, TimeSpan expiry)
        {
            this.directory.AddGroupMember(group, user, expiry);
        }
    }
}
