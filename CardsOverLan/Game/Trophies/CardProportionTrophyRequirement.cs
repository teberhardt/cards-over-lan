using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game.Trophies
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class CardProportionTrophyRequirement : TrophyRequirement
	{
		[JsonProperty("flags", Required = Required.Always)]
		private readonly List<string> _contentFlags = new List<string>();

		[JsonProperty("winning")]
		public bool Winning { get; set; }

		[JsonProperty("percent")]
		[DefaultValue(50)]
		public int Percent { get; set; } = 50;

		[JsonProperty("maximum")]
		public bool Maximum { get; set; } = false;

		public override bool CheckPlayer(Player player)
		{
			int totalCards = 0;
			int eligibleCards = 0;
			foreach (var play in player.GetPreviousPlays())
			{
				totalCards += play.PromptCard.PickCount;
				if (Winning && !play.Winning) continue;
				foreach (var card in play.GetCards())
				{
					if (_contentFlags.Any(f => card.ContainsContentFlags(f)))
					{
						eligibleCards++;
					}
				}
			}
			return Maximum ? eligibleCards * 100 / totalCards <= Percent : eligibleCards * 100 / totalCards >= Percent;
		}
	}
}
