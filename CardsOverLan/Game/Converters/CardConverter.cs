using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;

namespace CardsOverLan.Game.Converters
{
	class CardConverter : JsonConverter
	{
		private static readonly JsonSerializer _serializer = new JsonSerializer { ContractResolver = new BaseSpecifiedConcreteClassConverter() };

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

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var o = JToken.ReadFrom(reader) as JObject;
			if (o == null) throw new ArgumentException("No object found at current reader position.");
			var id = o["id"]?.Value<string>();
			if (id == null) throw new ArgumentException("Object is missing 'id' property.");

			if (id.StartsWith("w_"))
			{
				return o.ToObject<WhiteCard>(_serializer);
			}
			else if (id.StartsWith("b_"))
			{
				return o.ToObject<BlackCard>(_serializer);
			}
			return null;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			_serializer.Serialize(writer, value);
		}
	}
}
