using System.IO;

namespace Lithnet.AccessManager.Server.UI
{
    public class AppPathProvider : IAppPathProvider
    {
        public AppPathProvider()
        {
            this.AppPath = @"D:\dev\git\lithnet\laps-web\src\Lithnet.Laps.Web\Lithnet.AccessManager.Web";
            this.TemplatesPath = $"{AppPath}\\NotificationTemplates";
            this.ConfigFile = $"{AppPath}\\appsettings.json";
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
