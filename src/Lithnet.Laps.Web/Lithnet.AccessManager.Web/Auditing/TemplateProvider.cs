using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using NLog;

namespace Lithnet.AccessManager.Web.Internal
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
                string path = env.ResolvePath(templateNameOrPath, "NotificationTemplates");

                if (path == null || !File.Exists(path))
                {
                    throw new FileNotFoundException("The template file was not found", path);
                }

                string text = File.ReadAllText(path);

                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch (Exception ex)
            {
                this.logger.LogEventError(EventIDs.ErrorLoadingTemplateResource, "Could not load template file", ex);
                throw;
            }
        }
    }
}