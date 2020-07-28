using System.Collections.Generic;
using System.IO;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class JsonFileTargetsProvider : IJsonTargetsProvider
    {
        private readonly JsonFileTargetsProviderOptions options;

        private readonly IWebHostEnvironment env;

        public JsonFileTargetsProvider(IOptionsSnapshot<JsonFileTargetsProviderOptions> options, IWebHostEnvironment env)
        {
            this.options = options.Value;
            this.env = env;
        }

        private IList<IJsonTarget> targets;

        public IList<IJsonTarget> Targets
        {
            get
            {
                if (targets == null)
                {
                    if (this.options.Enabled)
                    {
                        string path = Path.Combine(this.env.ContentRootPath, this.options.AuthorizationFile);
                        if (!File.Exists(path))
                        {
                            throw new FileNotFoundException($"The JSON authorization file was not found: {path}");
                        }

                        var targetFile = JsonConvert.DeserializeObject<JsonTargets>(File.ReadAllText(path));

                        this.targets = targetFile?.Targets ?? new List<IJsonTarget>();
                    }
                    else
                    {
                        this.targets = new List<IJsonTarget>();
                    }
                }

                return targets;
            }
        }
    }
}