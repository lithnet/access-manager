using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    public static class ApplicationContextProvider
    {
        public static string AppPath { get; } = @"D:\dev\git\lithnet\laps-web\src\Lithnet.Laps.Web\Lithnet.AccessManager.Web";

        public static string TemplatesPath { get; } = $"{AppPath}\\NotificationTemplates";

        public static string ScriptsPath { get; } = $"{AppPath}\\Scripts";

        public static string WwwRootPath { get; } = $"{AppPath}\\wwwroot";

        public static string ImagesPath { get; } = $"{AppPath}\\wwwroot\\images";

        public static string ConfigFile { get; } = $"{AppPath}\\appsettings.json";
    }
}
