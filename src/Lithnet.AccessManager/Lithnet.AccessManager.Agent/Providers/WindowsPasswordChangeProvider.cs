using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    public class WindowsPasswordChangeProvider : IPasswordChangeProvider
    {
        private readonly ILocalSam sam;

        public WindowsPasswordChangeProvider(ILocalSam sam)
        {
            this.sam = sam;
        }

        public string GetAccountName()
        {
            return this.sam.GetBuiltInAdministratorAccountName();
        }

        public void ChangePassword(string password)
        {
            var sid = this.sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);
            this.sam.SetLocalAccountPassword(sid, password);
        }

        public void EnsureEnabled()
        {
            var sid = this.sam.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);
            this.sam.EnableLocalAccount(sid);
        }
    }
}