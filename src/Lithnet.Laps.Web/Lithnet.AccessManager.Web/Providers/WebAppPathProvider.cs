using System.IO;
using Microsoft.Extensions.Hosting;

namespace Lithnet.AccessManager.Web.Internal
{
    public class WebAppPathProvider : IAppPathProvider
    {
        private readonly IHostEnvironment env;

        public WebAppPathProvider(IHostEnvironment env)
        {
            this.env = env;
            this.AppPath = env.ContentRootPath;
            this.TemplatesPath = $"{AppPath}\\NotificationTemplates";
            this.ConfigFile = $"{AppPath}\\appsettings.json";
            this.HostingConfigFile = $"{AppPath}\\apphost.json";
            this.ScriptsPath = $"{AppPath}\\Scripts";
            this.WwwRootPath = $"{AppPath}\\wwwroot";
            this.ImagesPath = $"{AppPath}\\wwwroot\\images";
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
