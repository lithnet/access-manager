using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lithnet.AccessManager.Agent
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IHostEnvironment environment;
        private readonly IOptionsMonitor<T> options;
        private readonly string section;
        private readonly string file;

        public WritableOptions(
            IHostEnvironment environment,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            this.environment = environment;
            this.options = options;
            this.section = section;
            this.file = file;
        }

        public T Value => this.options.CurrentValue;

        public T Get(string name) => this.options.Get(name);

        public void Update(Action<T> applyChanges)
        {
            IFileProvider fileProvider = this.environment.ContentRootFileProvider;
            IFileInfo fileInfo = fileProvider.GetFileInfo(this.file);
            string physicalPath = fileInfo.PhysicalPath;

            JObject jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath));
            T sectionObject = jObject.TryGetValue(this.section, out JToken s) ?
                JsonConvert.DeserializeObject<T>(s.ToString()) : (this.Value ?? new T());

            applyChanges(sectionObject);

            jObject[this.section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(physicalPath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }
    }
}