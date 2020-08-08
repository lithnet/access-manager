using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using NetFwTypeLib;
using SslCertBinding.Net;

namespace Lithnet.AccessManager.Server.UI
{
    public class HostingSettingsRollbackContext
    {
        public bool StartingUnconfigured { get; set; }

        public List<Action> RollbackActions { get; set; } = new List<Action>();

        public bool Rollback(ILogger logger)
        {
            bool success = true;

            foreach (var action in this.RollbackActions.Reverse<Action>())
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIDs.UIConfigurationRollbackError, ex, "An error occurred rolling back a configuration change");
                    success = false;
                }
            }

            this.RollbackActions.Clear();
            return success;
        }
    }
}
