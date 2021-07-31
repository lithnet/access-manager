using Lithnet.AccessManager.Agent.Authentication;
using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Lithnet.AccessManager.Agent.Shared;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Agent
{
    public static class HostBuilderExtensions
    {
        public static void ConfigureWritable<T>(this IServiceCollection services, IConfigurationSection section, string file = "appsettings.json") where T : class, new()
        {
            services.Configure<T>(section);
            services.AddSingleton<IWritableOptions<T>>(provider =>
            {
                IHostEnvironment environment = provider.GetService<IHostEnvironment>();
                IOptionsMonitor<T> options = provider.GetService<IOptionsMonitor<T>>();
                return new WritableOptions<T>(environment, options, section.Key, file);
            });
        }

        public static IHostBuilder ConfigureAccessManagerAgent(this IHostBuilder builder)
        {
            builder.ConfigureServices((hostContext, services) =>
             {
                 IConfiguration configuration = hostContext.Configuration;

                 services.AddHostedService<Worker>();
                 services.AddHttpClient(Constants.HttpClientAuthAnonymous, (serviceProvider, c) =>
                 {
                     c.DefaultRequestHeaders.Add("Accept", "application/json");
                     c.DefaultRequestHeaders.Add("User-Agent", $"Lithnet Access Manager Agent {Assembly.GetExecutingAssembly().GetName().Version}");
                     var settings = serviceProvider.GetRequiredService<IAgentSettings>();
                     c.BaseAddress = new Uri($"https://{settings.Server.Trim()}/api/v1.0/");
                 });

                 if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                 {
                     services.AddHttpClient(Constants.HttpClientAuthIwa, (serviceProvider, c) =>
                     {
                         c.DefaultRequestHeaders.Add("Accept", "application/json");
                         c.DefaultRequestHeaders.Add("User-Agent", $"Lithnet Access Manager Agent {Assembly.GetExecutingAssembly().GetName().Version}");
                         var settings = serviceProvider.GetRequiredService<IAgentSettings>();
                         c.BaseAddress = new Uri($"https://{settings.Server.Trim()}/api/v1.0/");
                     }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                     {
                         AllowAutoRedirect = false,
                         UseDefaultCredentials = true,
                     });
                 }

                 services.AddHttpClient(Constants.HttpClientAuthBearer, (serviceProvider, c) =>
                 {
                     c.DefaultRequestHeaders.Add("Accept", "application/json");
                     c.DefaultRequestHeaders.Add("User-Agent", $"Lithnet Access Manager Agent {Assembly.GetExecutingAssembly().GetName().Version}");
                     var settings = serviceProvider.GetRequiredService<IAgentSettings>();
                     c.BaseAddress = new Uri($"https://{settings.Server.Trim()}/api/v1.0/");
                 })
                 .AddHttpMessageHandler<BearerTokenHandler>();

                 services.AddSingleton(AgentJsonSettings.JsonSerializerDefaults);

                 services.AddSingleton<X509TokenProvider>();
                 services.AddSingleton<ITokenProvider, TokenProvider>();
                 services.AddTransient<BearerTokenHandler>();
                 services.AddSingleton<ITokenClaimProvider, TokenClaimProvider>();
                 services.AddTransient<IAgentCheckInProvider, AgentCheckInProvider>();
                 services.AddTransient<IPasswordStorageProvider, AmsApiPasswordStorageProvider>();
                 services.AddTransient<IRegistrationProvider, RegistrationProvider>();
                 services.AddTransient<IAmsApiHttpClient, AmsApiHttpClient>();
                 services.AddTransient<IClientAssertionProvider, ClientAssertionProvider>();

                 services.AddTransient<AmsLapsAgent>();
                 services.AddTransient<IPasswordGenerator, RandomPasswordGenerator>();
                 services.AddSingleton(RandomNumberGenerator.Create());
                 services.AddSingleton<IRandomValueGenerator, RandomValueGenerator>();

                 services.AddTransient<IEncryptionProvider, EncryptionProvider>();

             });

            return builder;
        }
    }
}