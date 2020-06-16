using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Web.Internal
{
    public class JsonListInterfaceConverter<TImplementation, TAbstract> : JsonConverter //where TImplementation : TAbstract
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(TAbstract);

        public override object ReadJson(JsonReader reader, Type type, Object value, JsonSerializer jser)
            => jser.Deserialize<List<TImplementation>>(reader).Cast<TAbstract>().ToList();

        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer jser)
            => jser.Serialize(writer, value);
    }
}
