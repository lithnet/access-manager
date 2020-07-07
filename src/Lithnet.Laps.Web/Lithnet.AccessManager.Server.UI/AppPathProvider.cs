using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ControlzEx.Standard;

namespace Lithnet.AccessManager.Server.UI
{
    public static class AppPathProvider
    {
        public static string AppPath { get; } = @"D:\dev\git\lithnet\laps-web\src\Lithnet.Laps.Web\Lithnet.AccessManager.Web";

        public static string TemplatesPath { get; } = $"{AppPath}\\NotificationTemplates";

        public static string ScriptsPath { get; } = $"{AppPath}\\Scripts";

        public static string WwwRootPath { get; } = $"{AppPath}\\wwwroot";

        public static string ImagesPath { get; } = $"{AppPath}\\wwwroot\\images";

        public static string ConfigFile { get; } = $"{AppPath}\\appsettings.json";

        public static string GetRelativePath(string file, string basePath)
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

        public static string GetFullPath(string file, string basePath)
        {
            if (Path.IsPathFullyQualified(file))
            {
                return file;
            }

            return Path.Combine(basePath, file);
        }
    }
}
