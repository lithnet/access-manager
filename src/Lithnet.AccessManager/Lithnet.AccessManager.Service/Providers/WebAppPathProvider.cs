using System.IO;
using Lithnet.AccessManager.Server;
using Microsoft.Extensions.Hosting;

namespace Lithnet.AccessManager.Service
{
    public class WebAppPathProvider : IAppPathProvider
    {
        public WebAppPathProvider(IHostEnvironment env, IRegistryProvider registryProvider)
        {
            string appPath = registryProvider.BasePath ?? env.ContentRootPath;
            string configPath = registryProvider.ConfigPath ?? Path.Combine(appPath, "config");
            
            configPath = configPath.TrimEnd('\\');
            this.AppPath = appPath.TrimEnd('\\');

            this.TemplatesPath = $"{configPath}\\audit-templates";
            this.ConfigFile = $"{configPath}\\appsettings.json";
            this.HostingConfigFile = $"{configPath}\\apphost.json";
            this.ScriptsPath = $"{configPath}\\scripts";
            this.LogoPath = $"{configPath}\\logo.png";
            this.DbPath = $"{configPath}\\db";
        }

        public string AppPath { get; }

        public string TemplatesPath { get; }

        public string ScriptsPath { get; }

        public string LogoPath { get; }

        public string ConfigFile { get; }

        public string HostingConfigFile { get; }
        
        public string DbPath { get; }

        public string GetRelativePath(string file, string basePath)
        {
            string selectedPath = Path.GetDirectoryName(file);

            if (string.Equals(selectedPath, basePath))
            {
                return Path.GetFileName(file);
            }
            else
            {
                return file;
            }
        }

        public string GetFullPath(string file, string basePath)
        {
            if (Path.IsPathFullyQualified(file))
            {
                return file;
            }

            return Path.Combine(basePath, file);
        }
    }
}
