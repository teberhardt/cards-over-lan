using CardsOverLan.Game.Bots;
using CardsOverLan.Game.Converters;
using CardsOverLan.Game.Trophies;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class Pack
	{
		private const string DefaultName = "Untitled Pack";
		private const string DefaultId = "untitled";

		private readonly Dictionary<string, BlackCard> _blackCards;
		private readonly Dictionary<string, WhiteCard> _whiteCards;

		[JsonProperty("cards")]
		private readonly List<Card> _cards;

		[JsonProperty("taunts")]
		private readonly List<Taunt> _taunts;

		[JsonProperty("trophies")]
		private readonly List<Trophy> _trophies;

		[JsonProperty("name", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultName)]
		public string Name { get; private set; } = DefaultName;

		[JsonProperty("id", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultId)]
		public string Id { get; private set; } = DefaultId;

		[JsonProperty("accent_color", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		public string AccentColor { get; private set; }

		[JsonProperty("accent_background", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		public string AccentBackground { get; private set; }

		[JsonProperty("accent_text_decoration", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		public string AccentTextDecoration { get; private set; }

		[JsonProperty("accent_font_style", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		public string AccentFontStyle { get; private set; }

		[JsonProperty("accent_font_weight", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		public string AccentFontWeight { get; private set; }

		public Pack()
		{
			_blackCards = new Dictionary<string, BlackCard>();
			_whiteCards = new Dictionary<string, WhiteCard>();
			_cards = new List<Card>();
			_trophies = new List<Trophy>();
			_taunts = new List<Taunt>();
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext sc)
		{
			foreach (var card in _cards)
			{
				card.Owner = this;

				if (card is WhiteCard whiteCard)
				{
					_whiteCards[card.ID] = whiteCard;
				}
				else if (card is BlackCard blackCard)
				{
					_blackCards[card.ID] = blackCard;
				}
			}
		}

		public IEnumerable<Trophy> GetTrophies() => _trophies.AsEnumerable();

		public WhiteCard GetWhiteCard(string id) => _whiteCards.TryGetValue(id, out var card) ? card : null;

		public BlackCard GetBlackCard(string id) => _blackCards.TryGetValue(id, out var card) ? card : null;

		public IEnumerable<WhiteCard> GetWhiteCards() => _whiteCards.Values;

		public IEnumerable<BlackCard> GetBlackCards() => _blackCards.Values;

		public IEnumerable<Card> GetAllCards() => _cards.AsEnumerable();

		public IEnumerable<Taunt> GetTaunts() => _taunts.AsEnumerable();

		public override string ToString() => $"{Id}: {_cards.Count} ({_blackCards.Count}Q, {_whiteCards.Count}A) cards, {_trophies.Count} trophies, {_taunts.Count} taunts";
	}
}
