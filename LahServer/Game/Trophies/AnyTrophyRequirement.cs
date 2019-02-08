using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game.Trophies
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class AnyTrophyRequirement : TrophyRequirement
	{
		[JsonProperty("requirements", Required = Required.Always)]
		private readonly List<TrophyRequirement> _reqs = new List<TrophyRequirement>();

		public override bool CheckPlayer(LahPlayer player)
		{
			return _reqs.Any(r => r != null && r.CheckPlayer(player));
		}
	}
}
