using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LahServer.Game.Trophies
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class Trophy
	{
        [JsonProperty("requirements", Required = Required.Always)]
        private readonly List<TrophyRequirement> _reqs;

        [JsonProperty("id", Required = Required.Always)]
		public string Id { get; private set; }

        [JsonProperty("name", Required = Required.Always)]
        public LocalizedString Name { get; private set; }

        [JsonProperty("desc", Required = Required.Always)]
        public LocalizedString Description { get; private set; }

        public IEnumerable<TrophyRequirement> GetRequirements()
        {
            foreach(var req in _reqs)
            {
                yield return req;
            }
        }

        public bool IsPlayerEligible(LahPlayer player)
        {
            foreach(var req in _reqs)
            {
                if (req == null) continue;
                if (!req.CheckPlayer(player)) return false;
            }
            return true;
        }

        public override string ToString() => Name.ToString();
    }
}
