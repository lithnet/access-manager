using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Auditing
{
    public class PowershellNotificationChannel : NotificationChannel<PowershellNotificationChannelDefinition>
    {
        private readonly ILogger logger;

        private readonly IAppPathProvider env;

        private readonly NLog.ILogger nlogger = NLog.LogManager.GetCurrentClassLogger();

        public override string Name => "powershell";
        
        protected override IList<PowershellNotificationChannelDefinition> NotificationChannelDefinitions { get; }

        private PowerShell powershell;

        public PowershellNotificationChannel(ILogger<PowershellNotificationChannel> logger, IOptions<AuditOptions> auditSettings, IAppPathProvider env, ChannelWriter<Action> queue)
            : base(logger, queue)
        {
            this.logger = logger;
            this.NotificationChannelDefinitions = auditSettings.Value.NotificationChannels.Powershell;
            this.env = env;
        }

        protected override void Send(AuditableAction action, Dictionary<string, string> tokens, PowershellNotificationChannelDefinition settings)
        {
            if (powershell == null)
            {
                this.InitializePowerShellSession(settings);
            }

            this.powershell.ResetState();
            this.powershell
                .AddCommand("Write-AuditLog")
                    .AddParameter("tokens", tokens)
                    .AddParameter("isSuccess", action.IsSuccess)
                    .AddParameter("logger", nlogger);

            Task task = new Task(() =>
            {
                var results = this.powershell.Invoke();
                this.powershell.ThrowOnPipelineError();
            });

            task.Start();
            if (!task.Wait(TimeSpan.FromSeconds(settings.TimeOut)))
            {
                throw new TimeoutException("The PowerShell script did not complete within the configured time");
            }

            if (task.IsFaulted)
            {
                if (task.Exception != null) throw task.Exception;
                throw new AccessManagerException("The task failed");
            }
        }


        private void InitializePowerShellSession(PowershellNotificationChannelDefinition settings)
        {
            string path = this.env.GetFullPath(settings.Script, env.ScriptsPath);

            if (path == null || !File.Exists(path))
            {
                throw new FileNotFoundException("The PowerShell script was not found", path);
            }

            powershell = PowerShell.Create();
            powershell.AddScript(File.ReadAllText(path));
            powershell.Invoke();

            if (powershell.Runspace.SessionStateProxy.InvokeCommand.GetCommand("Write-AuditLog", CommandTypes.All) == null)
            {
                throw new NotSupportedException("The PowerShell script must contain a function called 'Write-AuditLog'");
            }

            this.logger.LogTrace($"The PowerShell script was successfully initialized");
        }
    }
}