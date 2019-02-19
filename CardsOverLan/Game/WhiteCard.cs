using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;

namespace CardsOverLan.Game
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class WhiteCard : Card
    {
        [JsonProperty("tier", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Tier { get; set; }

        [JsonProperty("tier_cost", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int TierCost { get; set; }

        [JsonProperty("next_tier_id", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("")]
        public string NextTierId { get; set; } = "";

        public override string ToString() => GetContent(CultureInfo.CurrentCulture.IetfLanguageTag) ?? GetContent(DefaultLocale) ?? "???";
    }
}
