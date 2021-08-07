using Microsoft.Extensions.Logging;
using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Lithnet.Extensions.Hosting.Launchd
{
    public class LaunchdLifetime : IHostLifetime, IDisposable
    {
        private readonly ManualResetEvent _shutdownBlock = new ManualResetEvent(false);
        private CancellationTokenRegistration _applicationStartedRegistration;
        private CancellationTokenRegistration _applicationStoppingRegistration;

        public LaunchdLifetime(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory)
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
        }

        private IHostApplicationLifetime ApplicationLifetime { get; }

        private IHostEnvironment Environment { get; }

        private ILogger Logger { get; }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            _applicationStartedRegistration = ApplicationLifetime.ApplicationStarted.Register(state =>
            {
                ((LaunchdLifetime)state).OnApplicationStarted();
            },
            this);
            _applicationStoppingRegistration = ApplicationLifetime.ApplicationStopping.Register(state =>
            {
                ((LaunchdLifetime)state).OnApplicationStopping();
            },
            this);

            // launchd sends SIGTERM to stop the service, but on macOS this doesn't seem to trigger ProcessExit as the documentation says it should
            // AssemblyLoadContext unloading does fire however.
            //AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AssemblyLoadContext.Default.Unloading += this.AssemblyLoadContext_Unloading;

            return Task.CompletedTask;
        }


        private void OnApplicationStarted()
        {
            Logger.LogInformation("Application started via launchd. Hosting environment: {EnvironmentName}; Content root path: {ContentRoot}",
                Environment.EnvironmentName, Environment.ContentRootPath);
        }

        private void OnApplicationStopping()
        {
            Logger.LogInformation("Application is shutting down (launchd)...");
        }

        private void AssemblyLoadContext_Unloading(AssemblyLoadContext obj)
        {
            ApplicationLifetime.StopApplication();
            Logger.LogTrace("AssemblyLoadContext unloading has been called");
            _shutdownBlock.WaitOne();

            // On Linux if the shutdown is triggered by SIGTERM then that's signaled with the 143 exit code.
            // Suppress that since we shut down gracefully. https://github.com/dotnet/aspnetcore/issues/6526
            System.Environment.ExitCode = 0;
        }

        public void Dispose()
        {
            _shutdownBlock.Set();

            AssemblyLoadContext.Default.Unloading -= this.AssemblyLoadContext_Unloading;

            _applicationStartedRegistration.Dispose();
            _applicationStoppingRegistration.Dispose();
        }
    }
}