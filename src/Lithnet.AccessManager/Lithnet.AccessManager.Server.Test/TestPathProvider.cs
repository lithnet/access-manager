using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Service.Internal
{
    public class TestPathProvider : IAppPathProvider
    {
        public TestPathProvider()
        {
            this.AppPath = Environment.CurrentDirectory;
            this.TemplatesPath = $"{AppPath}\\TestData\\NotificationTemplates";
            this.ConfigFile = $"{AppPath}\\appsettings.json";
            this.HostingConfigFile = $"{AppPath}\\apphost.json";
            this.ScriptsPath = $"{AppPath}\\TestData\\Scripts";
            this.WwwRootPath = $"{AppPath}\\wwwroot";
            this.ImagesPath = $"{AppPath}\\wwwroot\\images";
            this.LogoPath = $"{AppPath}\\logo.png";
            this.DbPath = $"{AppPath}\\db";
        }

        public string AppPath { get; }

        public string TemplatesPath { get; }

        public string ScriptsPath { get; }

        public string WwwRootPath { get; }

        public string ImagesPath { get; }

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
