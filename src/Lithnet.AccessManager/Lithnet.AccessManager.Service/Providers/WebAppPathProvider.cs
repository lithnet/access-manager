using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Service
{
    public class WebAppPathProvider : IAppPathProvider
    {
        public WebAppPathProvider(IHostEnvironment env)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(Constants.ParametersKey, false);
            string appPath = key?.GetValue("BasePath", null) as string ?? env.ContentRootPath;
            string configPath = key?.GetValue("ConfigPath", null) as string ?? Path.Combine(appPath, "config");
            string wwwRootPath = key?.GetValue("WwwRootPath", null) as string ?? Path.Combine(appPath, "wwwroot");

            this.AppPath = appPath.TrimEnd('\\');
            this.TemplatesPath = $"{configPath}\\audit-templates";
            this.ConfigFile = $"{configPath}\\appsettings.json";
            this.HostingConfigFile = $"{configPath}\\apphost.json";
            this.ScriptsPath = $"{configPath}\\scripts";
            this.WwwRootPath = wwwRootPath;
            this.ImagesPath = $"{wwwRootPath}\\images";
            this.LogoPath = $"{configPath}\\logo.png";
        }

        public string AppPath { get; }

        public string TemplatesPath { get; }

        public string ScriptsPath { get; }

        public string WwwRootPath { get; }

        public string ImagesPath { get; }
        public string LogoPath { get; }

        public string ConfigFile { get; }
        
        public string HostingConfigFile { get; }

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
