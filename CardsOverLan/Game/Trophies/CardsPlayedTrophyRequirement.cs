using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CardsOverLan.Game.Trophies
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class CardsPlayedTrophyRequirement : TrophyRequirement
    {
        [JsonProperty("cards", Required = Required.Always)]
        private readonly List<string> _cardIds = new List<string>();

        [JsonProperty("winner")]
        public bool Winning { get; set; }

        [JsonProperty("single_play")]
        public bool SinglePlay { get; set; }

        public override bool CheckPlayer(Player player)
        {
            var requiredCards = _cardIds.ToArray();
            var findings = new bool[_cardIds.Count];
            foreach (var play in player.GetPreviousPlays())
            {
                foreach (var card in play.GetCards())
                {
                    for (var i = 0; i < requiredCards.Length; i++)
                    {
                        if (card.Id == requiredCards[i])
                        {
                            findings[i] = true;
                        }
                    }
                }
                // ReSharper disable once InvertIf
                if (SinglePlay)
                {
                    if (findings.All(f => f))
                    {
                        return true;
                    }

                    for (var i = 0; i < findings.Length; i++)
                    {
                        findings[i] = false;
                    }
                }
            }
            return findings.All(f => f);
        }
    }
}