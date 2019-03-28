using CardsOverLan.Game.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CardsOverLan.Game
{
	[ClientObjectPolicy(ClientObjectPolicyType.OptIn)]
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	[JsonConverter(typeof(CardConverter))]
	public abstract class Card
	{
		protected const string DefaultLocale = "en";

		private string _id;

		[ClientFacing]
		[JsonProperty("content")]
		private readonly Dictionary<string, string> _content = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

		private readonly List<string> _languageFamilies = new List<string>();

		[ClientFacing]
		[JsonProperty("id")]
		public string ID
		{
			get => _id;
			set
			{
				if (String.IsNullOrWhiteSpace(value))
					throw new ArgumentException("Card ID cannot be null nor empty.");
				if (!value.All(c => c == '_' || Char.IsLetterOrDigit(c) || c == '/' || c == '+' || c == '='))
					throw new ArgumentException("Card IDs can only contain underscores, alphanumeric charactrs, and those allowed by Base64.");
				_id = value;
			}
		}

		public Pack Owner { get; internal set; }

		public bool IsCustom { get; private set; }

		[JsonProperty("flags")]
		[DefaultValue("")]
		public string ContentFlags { get; private set; } = "";

		public void AddContent(string languageCode, string content) => _content[languageCode] = content;

		public string GetContent(string languageCode) => String.IsNullOrWhiteSpace(languageCode) || !_content.TryGetValue(languageCode, out var c) ? null : c;


		[OnDeserialized]
		private void OnDeserialized(StreamingContext sc)
		{
			_languageFamilies.AddRange(_content.Keys.Select(k => k.Split(new[] { '-' })[0]));
		}

		public static WhiteCard CreateCustom(string content)
		{
			if (String.IsNullOrWhiteSpace(content)) return null;
			var contentBytes = Encoding.UTF8.GetBytes(content);
			var base64ContentText = Convert.ToBase64String(contentBytes);
			var card = new WhiteCard
			{
				ID = $"custom_{base64ContentText}",
				IsCustom = true
			};
			card.AddContent(DefaultLocale, content);
			return card;
		}

		public bool SupportsLanguage(string langCode)
		{
			return langCode != null && (_content.ContainsKey(langCode) || _languageFamilies.Contains(langCode));
		}

		public bool ContainsContentFlags(string flags)
		{
			var searchFlagParts = flags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Select(s => (shouldMatch: !s.StartsWith("!"), flag: s.TrimStart('!'))).ToArray();
			var cardFlagParts = ContentFlags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
			for (int i = 0; i < searchFlagParts.Length; i++)
			{
				bool found = false;
				for (int j = 0; j < cardFlagParts.Length; j++)
				{
					if (cardFlagParts[j] == searchFlagParts[i].flag)
					{
						found = true;
						break;
					}
				}
				if (found != searchFlagParts[i].shouldMatch) return false;
			}
			return true;
		}

		public override string ToString() => IsCustom ? "(custom)" : ID;
	}
}
