using Lithnet.Extensions.Hosting.Launchd;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lithnet.Extensions.Hosting
{
    /// <summary>
    /// Extension methods for setting up <see cref="LaunchdLifetime" />.
    /// </summary>
    public static class LaunchdHostBuilderExtensions
    {
        public static IHostBuilder UseLaunchd(this IHostBuilder hostBuilder)
        {
            if (LaunchdHelpers.IsLaunchdService())
            {
                hostBuilder.ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IHostLifetime, LaunchdLifetime>();
                });
            }

            return hostBuilder;
        }
    }
}
