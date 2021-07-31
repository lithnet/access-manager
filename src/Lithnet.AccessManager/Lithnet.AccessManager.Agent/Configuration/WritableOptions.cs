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
        private readonly IOptionsMonitor<T> options;
        private readonly string section;
        private readonly string file;
        private object lockObject = new object();

        public WritableOptions(IHostEnvironment environment, IOptionsMonitor<T> options, string section, string file)
        {
            this.options = options;
            this.section = section;

            if (Path.IsPathRooted(file))
            {
                this.file = file;
            }
            else
            {
                IFileProvider fileProvider = environment.ContentRootFileProvider;
                IFileInfo fileInfo = fileProvider.GetFileInfo(file);
                this.file = fileInfo.PhysicalPath;
            }
        }

        public T Value => this.options.CurrentValue;

        public T Get(string name) => this.options.Get(name);

        public void Update(Action<T> applyChanges)
        {
            lock (lockObject)
            {
                var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(file));
                T sectionObject = jObject.TryGetValue(this.section, out JToken s) ?
                    JsonConvert.DeserializeObject<T>(s.ToString()) : (this.Value ?? new T());

                applyChanges(sectionObject);

                jObject[this.section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));

                Directory.CreateDirectory(Path.GetDirectoryName(this.file));
                File.WriteAllText(this.file, JsonConvert.SerializeObject(jObject, Formatting.Indented));
            }
        }
    }
}