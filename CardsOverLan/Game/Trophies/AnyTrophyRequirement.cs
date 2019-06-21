using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CardsOverLan.Game.Trophies
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class AnyTrophyRequirement : TrophyRequirement
    {
        [JsonProperty("requirements", Required = Required.Always)]
        private readonly List<TrophyRequirement> _reqs = new List<TrophyRequirement>();

        public override bool CheckPlayer(Player player)
        {
            return _reqs.Any(r => r != null && r.CheckPlayer(player));
        }
    }
}