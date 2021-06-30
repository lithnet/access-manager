using System.Threading;
using System.Windows;
using Microsoft.Extensions.Hosting;

namespace Lithnet.AccessManager.Server.UI
{
    public class WpfHostLifetime : IHostApplicationLifetime
    {
        private CancellationTokenSource appStarted = new CancellationTokenSource();
        private CancellationTokenSource appStopping = new CancellationTokenSource();
        private CancellationTokenSource appStopped = new CancellationTokenSource();

        public WpfHostLifetime()
        {
            Application.Current.Exit += this.App_Exit;
            Application.Current.Startup += this.App_Startup;
        }

        private void App_Startup(object sender, System.Windows.StartupEventArgs e)
        {
            this.appStarted.Cancel();
        }

        private void App_Exit(object sender, System.Windows.ExitEventArgs e)
        {
            this.appStopping.Cancel();
            this.appStopped.Cancel();
        }

        public void StopApplication()
        {
            Application.Current.Shutdown();
        }

        public CancellationToken ApplicationStarted => this.appStarted.Token;

        public CancellationToken ApplicationStopping => this.appStopping.Token;

        public CancellationToken ApplicationStopped => this.appStopped.Token;
    }
}