using System;
using System.IO;
using Lithnet.Laps.Web.App_LocalResources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration.UserSecrets;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public class TemplateProvider: ITemplateProvider
    {
        private readonly ILogger logger;

        private readonly IWebHostEnvironment env;

        public TemplateProvider(ILogger logger, IWebHostEnvironment env)
        {
            this.env = env;
            this.logger = logger;
        }

        public string GetTemplate(string templateNameOrPath)
        {
            try
            {
                string path = env.ResolvePath(templateNameOrPath, "app_data\\templates");

                if (path == null || !File.Exists(path))
                {
                    throw new FileNotFoundException("The template file was not found", path);
                }

                string text = File.ReadAllText(path);

                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Could not load template file");
                throw;
            }
        }
    }
}