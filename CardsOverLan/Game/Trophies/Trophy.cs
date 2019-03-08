using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game.Trophies
{
	[ClientObjectPolicy(ClientObjectPolicyType.OptOut)]
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class Trophy
	{
		[ClientIgnore]
		[JsonProperty("requirements", Required = Required.Always)]
		private readonly List<TrophyRequirement> _reqs;

		[JsonProperty("id", Required = Required.Always)]
		public string Id { get; private set; }

		[JsonProperty("name", Required = Required.Always)]
		public LocalizedString Name { get; private set; }

		[ClientIgnore]
		[JsonProperty("trophy_class", Required = Required.DisallowNull)]
		[DefaultValue("")]
		public string TrophyClass { get; private set; } = "";

		[JsonProperty("trophy_grade")]
		public int TrophyGrade { get; private set; }

		[JsonProperty("desc", Required = Required.Always)]
		public LocalizedString Description { get; private set; }

		public IEnumerable<TrophyRequirement> GetRequirements()
		{
			foreach (var req in _reqs)
			{
				yield return req;
			}
		}

		public bool IsPlayerEligible(Player player)
		{
			foreach (var req in _reqs)
			{
				if (req == null) continue;
				if (!req.CheckPlayer(player)) return false;
			}
			return true;
		}

		public override string ToString() => Name.ToString();
	}
}
