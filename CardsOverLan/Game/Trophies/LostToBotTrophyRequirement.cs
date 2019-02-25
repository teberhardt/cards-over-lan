using Newtonsoft.Json;
using System.Linq;

namespace CardsOverLan.Game.Trophies
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class LostToBotTrophyRequirement : TrophyRequirement
	{
		public override bool CheckPlayer(Player player)
		{
			if (player.IsAutonomous) return false;
			var winners = player.Game.GetWinningPlayers().ToArray();
			return !winners.Contains(player) && winners.Any(w => w.IsAutonomous);
		}
	}
}
