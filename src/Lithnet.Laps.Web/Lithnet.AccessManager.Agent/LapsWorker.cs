using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class LapsWorker 
    {
        private readonly ILogger<LapsWorker> logger;

        private readonly IDirectory directory;

        private readonly ISettingsProvider settings;

        public LapsWorker(ILogger<LapsWorker> logger, IDirectory directory, ISettingsProvider settings)
        {
            this.logger = logger;
            this.directory = directory;
            this.settings = settings;
        }

        public void DoCheck()
        {
            if (!this.settings.LapsEnabled)
            {
                return;
            }

            IComputer computer = this.directory.GetComputer();

            var lam =  this.directory.GetLamSettings(computer);

            if (lam.PasswordExpiry > DateTime.UtcNow)
            {
                this.ChangePassword(lam);
            }
        }

        private void ChangePassword(ILamSettings settings)
        {
            SecurityIdentifier localAdminSid = this.directory.GetWellKnownSid(WellKnownSidType.AccountAdministratorSid);
            

        }
    }
}
