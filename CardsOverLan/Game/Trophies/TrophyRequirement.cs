using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardsOverLan.Game.Converters;

namespace CardsOverLan.Game.Trophies
{
	[JsonConverter(typeof(TrophyRequirementConverter))]
	public abstract class TrophyRequirement
	{
		public TrophyRequirement()
		{
		}

		public abstract bool CheckPlayer(Player player);
	}
}
