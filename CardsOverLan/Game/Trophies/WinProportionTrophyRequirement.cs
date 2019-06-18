using Newtonsoft.Json;
using System.ComponentModel;

namespace CardsOverLan.Game.Trophies
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class WinProportionTrophyRequirement : TrophyRequirement
    {
        [JsonProperty("percent", Required = Required.Always)]
        [DefaultValue(50)]
        public int Percent { get; set; } = 50;

        [JsonProperty("maximum")]
        public bool Maximum { get; set; }

        [JsonProperty("inclusive")]
        public bool Inclusive { get; set; } = true;

        public override bool CheckPlayer(Player player)
        {
            var winCount = 0;
            var playCount = 0;
            foreach (var play in player.GetPreviousPlays())
            {
                playCount++;
                if (play.Winning) winCount++;
            }
            var percent = winCount * 100 / playCount;

            return Maximum
                ? Inclusive
                    ? percent <= Percent
                    : percent < Percent
                : Inclusive
                    ? percent >= Percent
                    : percent > Percent;
        }
    }
}