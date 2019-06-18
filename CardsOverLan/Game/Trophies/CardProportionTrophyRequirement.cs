using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CardsOverLan.Game.Trophies
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class CardProportionTrophyRequirement : TrophyRequirement
    {
        [JsonProperty("flags", Required = Required.Always)]
        private readonly List<string> _contentFlags = new List<string>();

        [JsonProperty("winning")]
        public bool Winning { get; set; }

        [JsonProperty("percent")]
        [DefaultValue(50)]
        public int Percent { get; set; } = 50;

        [JsonProperty("maximum")]
        public bool Maximum { get; set; }

        public override bool CheckPlayer(Player player)
        {
            var totalCards = 0;
            var eligibleCards = 0;
            foreach (var play in player.GetPreviousPlays())
            {
                totalCards += play.PromptCard.PickCount;
                if (Winning && !play.Winning) continue;
                eligibleCards += play.GetCards().Count(card => _contentFlags.Any(card.ContainsContentFlags));
            }
            return Maximum ? eligibleCards * 100 / totalCards <= Percent : eligibleCards * 100 / totalCards >= Percent;
        }
    }
}