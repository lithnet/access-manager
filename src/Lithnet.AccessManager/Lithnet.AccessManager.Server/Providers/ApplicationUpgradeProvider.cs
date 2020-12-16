using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public class ApplicationUpgradeProvider : IApplicationUpgradeProvider
    {
        private readonly ILogger<ApplicationUpgradeProvider> logger;

        private const string UrlProductVersionInfo = "https://lithnet.github.io/access-manager/version.json";

        public ApplicationUpgradeProvider(ILogger<ApplicationUpgradeProvider> logger)
        {
            this.logger = logger;
        }

        public async Task<AppVersionInfo> GetVersionInfo()
        {
            AppVersionInfo appVersionInfo = new AppVersionInfo();
            appVersionInfo.Status = VersionInfoStatus.Unknown;

            try
            {
                appVersionInfo.CurrentVersion = Assembly.GetEntryAssembly()?.GetName()?.Version;

                string appdata = await DownloadFile(UrlProductVersionInfo);

                if (appdata == null)
                {
                    this.logger.LogTrace("Version check response data from URL {url} was null", UrlProductVersionInfo);
                    return appVersionInfo;
                }

                PublishedVersionInfo versionInfo = JsonConvert.DeserializeObject<PublishedVersionInfo>(appdata);
                appVersionInfo.AvailableVersion = Version.Parse(versionInfo.CurrentVersion);
                appVersionInfo.UpdateUrl = versionInfo.UserUrl;
                appVersionInfo.ReleaseNotes = versionInfo.ReleaseNotes;

                if (appVersionInfo.AvailableVersion > appVersionInfo.CurrentVersion)
                {
                    appVersionInfo.Status = VersionInfoStatus.UpdateAvailable;
                }
                else
                {
                    appVersionInfo.Status = VersionInfoStatus.Current;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not get version update");
                appVersionInfo.Status = VersionInfoStatus.Failed;
            }

            this.logger.LogTrace("Update check returned {result}", appVersionInfo);

            return appVersionInfo;
        }

        private static async Task<string> DownloadFile(string url)
        {
            using HttpClientHandler handler = new HttpClientHandler
            {
                DefaultProxyCredentials = CredentialCache.DefaultCredentials
            };

            using HttpClient client = new HttpClient(handler);
            using HttpResponseMessage result = await client.GetAsync(url);

            result.EnsureSuccessStatusCode();
            return await result.Content.ReadAsStringAsync();
        }
    }
}
