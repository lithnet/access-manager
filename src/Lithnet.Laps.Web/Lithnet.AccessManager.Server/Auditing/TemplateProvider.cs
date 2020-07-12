using System;
using System.IO;
using Lithnet.AccessManager.Server.Extensions;
using NLog;

namespace Lithnet.AccessManager.Server.Auditing
{
    public class TemplateProvider: ITemplateProvider
    {
        private readonly ILogger logger;

        private readonly IAppPathProvider env;

        public TemplateProvider(ILogger logger, IAppPathProvider env)
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
                this.logger.LogEventError(EventIDs.ErrorLoadingTemplateResource, "Could not load template file", ex);
                throw;
            }
        }
    }
}