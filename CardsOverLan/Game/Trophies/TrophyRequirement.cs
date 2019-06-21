using Newtonsoft.Json;
using CardsOverLan.Game.Converters;

namespace CardsOverLan.Game.Trophies
{
    [JsonConverter(typeof(TrophyRequirementConverter))]
    public abstract class TrophyRequirement
    {
        public abstract bool CheckPlayer(Player player);
    }
}