using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lithnet.AccessManager.Agent
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IHostEnvironment _environment;
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;

        public WritableOptions(
            IHostEnvironment environment,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            this._environment = environment;
            this._options = options;
            this._section = section;
            this._file = file;
        }

        public T Value => this._options.CurrentValue;
        public T Get(string name) => this._options.Get(name);

        public void Update(Action<T> applyChanges)
        {
            var fileProvider = this._environment.ContentRootFileProvider;
            var fileInfo = fileProvider.GetFileInfo(this._file);
            var physicalPath = fileInfo.PhysicalPath;

            var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath));
            var sectionObject = jObject.TryGetValue(this._section, out JToken section) ?
                JsonConvert.DeserializeObject<T>(section.ToString()) : (this.Value ?? new T());

            applyChanges(sectionObject);

            jObject[this._section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(physicalPath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }
    }
}