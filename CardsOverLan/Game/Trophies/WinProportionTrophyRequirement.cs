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
	public sealed class WinProportionTrophyRequirement : TrophyRequirement
	{
		[JsonProperty("percent", Required = Required.Always)]
		[DefaultValue(50)]
		public int Percent { get; set; } = 50;

		[JsonProperty("maximum")]
		public bool Maximum { get; set; } = false;

		[JsonProperty("inclusive")]
		public bool Inclusive { get; set; } = true;

		public override bool CheckPlayer(Player player)
		{
			int winCount = 0;
			int playCount = 0;
			foreach (var play in player.GetPreviousPlays())
			{
				playCount++;
				if (play.Winning) winCount++;
			}
			int percent = winCount * 100 / playCount;

			return Maximum
				? Inclusive
					? percent <= Percent
					: percent < Percent
				: Inclusive
					? percent >= Percent
					: percent > Percent;
		}
	}
}
