using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CardsOverLan.Game.Converters
{
    internal class CardConverter : JsonConverter
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer
            {ContractResolver = new BaseSpecifiedConcreteClassConverter()};

        private sealed class BaseSpecifiedConcreteClassConverter : DefaultContractResolver
        {
            protected override JsonConverter ResolveContractConverter(Type objectType)
            {
                if (typeof(Card).IsAssignableFrom(objectType) && !objectType.IsAbstract) return null;
                return base.ResolveContractConverter(objectType);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Card);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (!(JToken.ReadFrom(reader) is JObject o)) throw new ArgumentException("No object found at current reader position.");
            var id = o["id"]?.Value<string>();
            if (id == null) throw new ArgumentException("Object is missing `id` property.");
            if (id.StartsWith("w_")) return o.ToObject<WhiteCard>(Serializer);
            return id.StartsWith("b_") ? o.ToObject<BlackCard>(Serializer) : null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Serializer.Serialize(writer, value);
        }
    }
}