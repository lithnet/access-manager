using Lithnet.AccessManager.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Lithnet.AccessManager.PowerShell
{
    internal static class DiServices
    {
        public static IServiceProvider Services { get; }

        public static T GetRequiredService<T>()
        {
            return Services.GetService<T>();
        }

        static DiServices()
        {
            Services = new ServiceCollection()
                .AddSingleton<IActiveDirectory, ActiveDirectory>()
                .AddSingleton<IDiscoveryServices, DiscoveryServices>()
                .AddSingleton<ICertificateProvider, CertificateProvider>()
                .AddSingleton<IEncryptionProvider, EncryptionProvider>()
                .AddSingleton<ILithnetAdminPasswordProvider, LithnetAdminPasswordProvider>()
                .AddSingleton<ILoggerFactory, NullLoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .BuildServiceProvider();
        }
    }
}
