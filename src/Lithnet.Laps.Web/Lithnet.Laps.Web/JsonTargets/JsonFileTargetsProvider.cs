using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.JsonTargets
{
    public class JsonFileTargetsProvider : IJsonTargetsProvider
    {
        private IList<JsonTarget> targets;

        public IList<JsonTarget> Targets
        {
            get
            {
                if (targets == null)
                {
                    string path = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/targets.json");
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException("The file 'app_data/targets.json' was not found");
                            
                    }

                    var targetFile = JsonConvert.DeserializeObject<JsonTargets>(File.ReadAllText(path));

                    this.targets = targetFile?.Targets ?? new List<JsonTarget>();
                }

                return targets;
            }
        }
    }
}