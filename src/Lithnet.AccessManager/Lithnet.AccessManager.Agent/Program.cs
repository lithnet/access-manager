using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

[assembly:InternalsVisibleTo("Lithnet.AccessManager.Agent.Test")]

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
                    services.AddTransient<IAgentSettings, AgentRegistrySettings>();
                    services.AddTransient<IJitSettings, JitRegistrySettings>();
                    services.AddTransient<IJitAgent, JitAgent>();
                    services.AddTransient<IJitAccessGroupResolver, JitAccessGroupResolver>();
                    services.AddTransient<ILapsSettings, LapsRegistrySettings>();
                    services.AddTransient<ILapsAgent, LapsAgent>();
                    services.AddTransient<ILocalSam, LocalSam>();
                    services.AddTransient<IAppPathProvider, AgentAppPathProvider>();
                    services.AddTransient<IPasswordGenerator, RandomPasswordGenerator>();
                    services.AddSingleton<RNGCryptoServiceProvider>();
                    services.AddTransient<IEncryptionProvider, EncryptionProvider>();
                    services.AddTransient<ICertificateProvider, CertificateProvider>();
                    services.AddTransient<IMsMcsAdmPwdProvider, MsMcsAdmPwdProvider>();
                    services.AddTransient<ILithnetAdminPasswordProvider, LithnetAdminPasswordProvider>();

                    services.AddLogging(builder =>
                    {
                        builder.AddNLog("nlog.config");
                        builder.AddEventLog(settings =>
                        {
                            settings.LogName = "Lithnet Access Manager";
                            settings.SourceName = "Lithnet Access Manager Agent";
                            settings.Filter = (x, y) => y >= LogLevel.Information;
                        });
                    });
                }
              ).UseWindowsService();
        }
    }
}