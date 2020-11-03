using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Threading.Channels;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.Auditing
{
    public class PowershellNotificationChannel : NotificationChannel<PowershellNotificationChannelDefinition>
    {
        public override string Name => "powershell";

        protected override IList<PowershellNotificationChannelDefinition> NotificationChannelDefinitions { get; }

        private readonly IPowerShellSessionProvider sessionProvider;

        public PowershellNotificationChannel(ILogger<PowershellNotificationChannel> logger, IOptionsSnapshot<AuditOptions> auditSettings, ChannelWriter<Action> queue, IPowerShellSessionProvider sessionProvider)
            : base(logger, queue)
        {
            this.NotificationChannelDefinitions = auditSettings.Value.NotificationChannels.Powershell;
            this.sessionProvider = sessionProvider;
        }

        protected override void Send(AuditableAction action, Dictionary<string, string> tokens, PowershellNotificationChannelDefinition settings)
        {
            PowerShell powershell = this.sessionProvider.GetSession(settings.Script, "Write-AuditLog");

            powershell.AddCommand("Write-AuditLog")
                .AddParameter("tokens", new System.Collections.Hashtable(tokens, StringComparer.OrdinalIgnoreCase))
                .AddParameter("isSuccess", action.IsSuccess);

            Task task = new Task(() =>
            {
                powershell.Invoke();
                powershell.ThrowOnPipelineError();
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
    }
}