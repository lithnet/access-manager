using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Lithnet.AccessManager.Agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddTransient<IDirectory, ActiveDirectory>();
                    services.AddTransient<IJitSettingsProvider, RegistrySettingsProvider>();
                    services.AddTransient<IJitWorker, JitWorker>();
                    services.AddTransient<IPasswordGenerator, RandomPasswordGenerator>();
                    services.AddSingleton<RNGCryptoServiceProvider>();
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        builder.AddNLog("nlog.config");
                    });
                }
              ).UseWindowsService();
        }
    }
}