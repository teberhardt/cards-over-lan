using CardsOverLan.Game.Trophies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace CardsOverLan.Game.Converters
{
	internal sealed class TrophyRequirementConverter : JsonConverter
	{
		private static readonly Dictionary<string, Type> ReqTypes = new Dictionary<string, Type>();
		private static readonly Dictionary<Type, string> ReqNames = new Dictionary<Type, string>();
		private static readonly BaseSpecifiedConcreteClassConverter Resolver = new BaseSpecifiedConcreteClassConverter();

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
			ReqTypes.Add(requirementName, requirementType);
			ReqNames.Add(requirementType, requirementName);
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
			var reqTypeName = token?["type"]?.Value<string>();
			if (reqTypeName == null) return null;
			if (!ReqTypes.TryGetValue(reqTypeName, out var reqType)) return null;
			return token.ToObject(reqType, new JsonSerializer { ContractResolver = Resolver }) as TrophyRequirement;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var token = JToken.FromObject(value, new JsonSerializer { ContractResolver = Resolver });
			if (ReqNames.TryGetValue(value.GetType(), out var reqTypeName))
			{
				token["type"] = reqTypeName;
			}
			token.WriteTo(writer);
		}
	}
}
