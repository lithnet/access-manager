using System;
using System.IO;
using Microsoft.Win32;

namespace Lithnet.AccessManager.Server.UI
{
    public class AppPathProvider : IAppPathProvider
    {
        public AppPathProvider()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(AccessManager.Constants.BaseKey, false);
            string appPath = key?.GetValue("BasePath", null) as string ?? key?.GetValue("Path", null) as string ?? Environment.CurrentDirectory;

            this.AppPath = appPath.TrimEnd('\\');
            this.TemplatesPath = $"{AppPath}\\NotificationTemplates";
            this.ConfigFile = $"{AppPath}\\appsettings.json";
            this.HostingConfigFile = $"{AppPath}\\apphost.json";
            this.ImagesPath = $"{AppPath}\\wwwroot\\images";
            this.ScriptsPath = $"{AppPath}\\Scripts";
            this.WwwRootPath = $"{AppPath}\\wwwroot";
        }

        public string AppPath { get; }

        public string TemplatesPath { get; } 

        public string ScriptsPath { get; } 

        public string WwwRootPath { get; } 

        public string ImagesPath { get; }

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
