using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

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
