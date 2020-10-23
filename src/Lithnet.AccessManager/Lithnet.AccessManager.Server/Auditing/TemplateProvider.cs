using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.Auditing
{
    public class TemplateProvider: ITemplateProvider
    {
        private readonly ILogger logger;

        private readonly IAppPathProvider env;

        public TemplateProvider(ILogger<TemplateProvider> logger, IAppPathProvider env)
        {
            this.env = env;
            this.logger = logger;
        }

        public string GetTemplate(string templateNameOrPath)
        {
            try
            {
                string path = env.GetFullPath(templateNameOrPath, env.TemplatesPath);

                if (path == null || !File.Exists(path))
                {
                    throw new FileNotFoundException("The template file was not found", path);
                }

                string text = File.ReadAllText(path);

                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.ErrorLoadingTemplateResource, ex, "Could not load template file");
                throw;
            }
        }
    }
}