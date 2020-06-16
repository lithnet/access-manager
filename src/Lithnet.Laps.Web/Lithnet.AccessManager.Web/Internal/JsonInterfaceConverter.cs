using System;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Web.Internal
{
    public class JsonInterfaceConverter<TImplementation, TAbstract> : JsonConverter where TImplementation : TAbstract
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(TAbstract);

        public override object ReadJson(JsonReader reader, Type type, Object value, JsonSerializer jser)
            => jser.Deserialize<TImplementation>(reader);

        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer jser)
            => jser.Serialize(writer, value);
    }
}
