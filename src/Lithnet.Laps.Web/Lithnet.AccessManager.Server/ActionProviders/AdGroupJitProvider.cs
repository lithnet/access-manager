using System;
using NLog;

namespace Lithnet.AccessManager
{
    public class AdGroupJitProvider : IJitProvider
    {
        private readonly IDirectory directory;

        public AdGroupJitProvider(IDirectory directory)
        {
            this.directory = directory;
        }

        public void GrantJitAccess(IComputer computer, IGroup group, IUser user, TimeSpan expiry)
        {
            if (this.directory.IsPamFeatureEnabled(group.Sid))
            {
                this.directory.AddGroupMember(group, user, expiry);
            }
            else
            {
                throw new NotImplementedException();
                //this.directory.CreateTtlGroup()
            }
        }
    }
}
