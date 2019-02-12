using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game.Trophies
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class AllTrophyRequirement : TrophyRequirement
	{
		[JsonProperty("requirements", Required = Required.Always)]
		private readonly List<TrophyRequirement> _reqs = new List<TrophyRequirement>();

		public override bool CheckPlayer(Player player)
		{
			return _reqs.Count > 0 && _reqs.All(r => r != null && r.CheckPlayer(player));
		}
	}
}
