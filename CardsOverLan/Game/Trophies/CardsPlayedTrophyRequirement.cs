using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game.Trophies
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class CardsPlayedTrophyRequirement : TrophyRequirement
	{
		[JsonProperty("cards", Required = Required.Always)]
		private readonly List<string> _cardIds = new List<string>();

		[JsonProperty("winner")]
		public bool Winning { get; set; }

		[JsonProperty("single_play")]
		public bool SinglePlay { get; set; }

		public override bool CheckPlayer(Player player)
		{
			var requiredCards = _cardIds.ToArray();
			var findings = new bool[_cardIds.Count];
			foreach (var play in player.GetPreviousPlays())
			{
				foreach (var card in play.GetCards())
				{
					for (int i = 0; i < requiredCards.Length; i++)
					{
						if (card.ID == requiredCards[i])
						{
							findings[i] = true;
						}
					}
				}
				if (SinglePlay)
				{
					if (findings.All(f => f))
					{
						return true;
					}
					else
					{
						for (int i = 0; i < findings.Length; i++)
						{
							findings[i] = false;
						}
					}
				}
			}
			return findings.All(f => f);
		}
	}
}
