using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CardsOverLan.Game
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class Card
    {
        [JsonProperty("content")]
        private readonly Dictionary<string, string> _content = new Dictionary<string, string>();

        [JsonProperty("id")]
        public string ID { get; internal set; }

        public Pack Owner { get; internal set; }

		public bool IsCustom { get; private set; }

        [JsonProperty("flags")]
        [DefaultValue("")]
        public string ContentFlags { get; private set; } = "";

		public void AddContent(string languageCode, string content) => _content[languageCode] = content;

        public string GetContent(string languageCode) => String.IsNullOrWhiteSpace(languageCode) || !_content.TryGetValue(languageCode, out var c) ? null : c;

		public static WhiteCard CreateCustom(string content)
		{
			if (String.IsNullOrWhiteSpace(content)) return null;
			var card = new WhiteCard
			{
				ID = $"custom: {content}",
				IsCustom = true
			};
			card.AddContent("en-US", content);
			return card;
		}

        public bool ContainsContentFlags(string flags)
        {
            var flagParts = flags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            var cardFlagParts = ContentFlags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            for(int i = 0; i < flagParts.Length; i++)
            {
                bool found = false;
                for(int j = 0; j < cardFlagParts.Length; j++)
                {
                    if (cardFlagParts[j] == flagParts[i])
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }
    }
}
