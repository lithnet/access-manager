using System.Drawing;
using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server
{
    public class DbUpgradeLogger : IUpgradeLog
    {
        private readonly ILogger<DbUpgradeLogger> logger;

        public DbUpgradeLogger(ILogger<DbUpgradeLogger> logger)
        {
            this.logger = logger;
        }

        public void WriteError(string format, params object[] args)
        {
            this.logger.LogError(EventIDs.DbUpgradeError, string.Format(format, args));
        }

        public void WriteInformation(string format, params object[] args)
        {
            this.logger.LogTrace(EventIDs.DbUpgradeInfo, string.Format(format, args));
        }

        public void WriteWarning(string format, params object[] args)
        {
            this.logger.LogWarning(EventIDs.DbUpgradeWarning, string.Format(format, args));
        }
    }
}
