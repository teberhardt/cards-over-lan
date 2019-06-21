using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CardsOverLan.Game.Converters
{
	internal class EnumNameConverter : JsonConverter
	{
		private static readonly Dictionary<Type, Dictionary<string, object>> NameToEnumMap;
		private static readonly Dictionary<Type, Dictionary<object, string>> EnumToNameMap;

		static EnumNameConverter()
		{
			NameToEnumMap = new Dictionary<Type, Dictionary<string, object>>();
			EnumToNameMap = new Dictionary<Type, Dictionary<object, string>>();
		}

		private static void RegisterEnum(Type enumType)
		{
			if (NameToEnumMap.ContainsKey(enumType)) return;

			var nameToEnum = NameToEnumMap[enumType] = new Dictionary<string, object>();
			var enumToName = EnumToNameMap[enumType] = new Dictionary<object, string>();

			var pairs = enumType.GetFields()
				.Where(f => f.FieldType == f.DeclaringType)
				.Select(f => (val: Enum.ToObject(enumType, f.GetRawConstantValue()), name: f.GetCustomAttribute<NameAttribute>()?.Name))
				.Where(pair => !string.IsNullOrWhiteSpace(pair.name));

			foreach (var (val, name) in pairs)
			{
				nameToEnum[name] = val;
				enumToName[val] = name;
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType.IsEnum;
		}

		public override bool CanWrite => true;

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var defaultValue = Activator.CreateInstance(objectType);
			RegisterEnum(objectType);
			var token = JToken.Load(reader);
			if (token == null) return defaultValue;
			var strValue = token.Value<string>();
			if (!NameToEnumMap.TryGetValue(objectType, out var map)) return defaultValue;
			return !map.TryGetValue(strValue, out var val) ? defaultValue : val;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}
			var type = value.GetType();
			RegisterEnum(type);
			if (EnumToNameMap.TryGetValue(type, out var map) && map.TryGetValue(value, out var str))
			{
				writer.WriteValue(str);
			}
			else
			{
				writer.WriteNull();
			}
		}
	}
}
