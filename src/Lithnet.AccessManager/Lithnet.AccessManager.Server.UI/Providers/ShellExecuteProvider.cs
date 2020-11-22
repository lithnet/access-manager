using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    public class ShellExecuteProvider : IShellExecuteProvider
    {
        private readonly ILogger logger;

        public ShellExecuteProvider(ILogger<ShellExecuteProvider> logger)
        {
            this.logger = logger;
        }

        public Task OpenWithShellExecute(string path)
        {
            if (path == null)
            {
                return Task.CompletedTask;
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
                MessageBox.Show($"Could not open the default handler\r\n{ex.Message}", "Could not open link", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }
    }
}
