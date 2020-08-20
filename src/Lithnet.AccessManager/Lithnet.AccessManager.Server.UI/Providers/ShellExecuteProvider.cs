using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    public class ShellExecuteProvider : IShellExecuteProvider
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger logger;

        public ShellExecuteProvider(IDialogCoordinator dialogCoordinator, ILogger<ShellExecuteProvider> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
        }

        public async Task OpenWithShellExecute(string path)
        {
            if (path == null)
            {
                return;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not open item");
                await dialogCoordinator.ShowMessageAsync(path, "Error", $"Could not open the default handler\r\n{ex.Message}");
            }
        }
    }
}
