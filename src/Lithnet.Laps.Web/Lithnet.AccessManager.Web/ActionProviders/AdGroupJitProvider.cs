using System;
using NLog;

namespace Lithnet.AccessManager.Web
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
            if (this.directory.IsPamFeatureEnabled(group.Sid))
            {
                this.directory.AddGroupMember(group, user, expiry);
            }
            else
            {
                //this.directory.CreateTtlGroup()
            }
        }
    }
}
