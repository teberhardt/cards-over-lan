using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game.Trophies
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class AllTrophyRequirement : TrophyRequirement
	{
		private readonly List<TrophyRequirement> _reqs = new List<TrophyRequirement>();

		public override bool CheckPlayer(LahPlayer player)
		{
			return _reqs.All(r => r != null && r.CheckPlayer(player));
		}
	}
}
