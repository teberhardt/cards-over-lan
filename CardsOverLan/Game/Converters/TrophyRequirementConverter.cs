using CardsOverLan.Game.Trophies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game.Converters
{
	internal sealed class TrophyRequirementConverter : JsonConverter
	{
		private static readonly Dictionary<string, Type> _reqTypes = new Dictionary<string, Type>();
		private static readonly Dictionary<Type, string> _reqNames = new Dictionary<Type, string>();
		private static readonly BaseSpecifiedConcreteClassConverter _resolver = new BaseSpecifiedConcreteClassConverter();

		private sealed class BaseSpecifiedConcreteClassConverter : DefaultContractResolver
		{
			protected override JsonConverter ResolveContractConverter(Type objectType)
			{
				if (typeof(TrophyRequirement).IsAssignableFrom(objectType) && !objectType.IsAbstract) return null;
				return base.ResolveContractConverter(objectType);
			}
		}

		static TrophyRequirementConverter()
		{
			RegisterRequirementType("card_proportion", typeof(CardProportionTrophyRequirement));
			RegisterRequirementType("cards_played", typeof(CardsPlayedTrophyRequirement));
			RegisterRequirementType("win_proportion", typeof(WinProportionTrophyRequirement));
			RegisterRequirementType("any", typeof(AnyTrophyRequirement));
			RegisterRequirementType("all", typeof(AllTrophyRequirement));
			RegisterRequirementType("lost_to_bot", typeof(LostToBotTrophyRequirement));
		}

		private static void RegisterRequirementType(string requirementName, Type requirementType)
		{
			_reqTypes.Add(requirementName, requirementType);
			_reqNames.Add(requirementType, requirementName);
		}

		public override bool CanRead => true;

		public override bool CanWrite => true;

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(TrophyRequirement);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var token = JToken.ReadFrom(reader) as JObject;
			if (token == null) return null;
			var reqTypeName = token["type"]?.Value<string>();
			if (reqTypeName == null) return null;
			if (!_reqTypes.TryGetValue(reqTypeName, out var reqType)) return null;
			return token.ToObject(reqType, new JsonSerializer { ContractResolver = _resolver }) as TrophyRequirement;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var token = JToken.FromObject(value, new JsonSerializer { ContractResolver = _resolver });
			if (_reqNames.TryGetValue(value.GetType(), out var reqTypeName))
			{
				token["type"] = reqTypeName;
			}
			token.WriteTo(writer);
		}
	}
}
