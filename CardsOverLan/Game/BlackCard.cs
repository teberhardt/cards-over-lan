using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;

namespace CardsOverLan.Game
{
	public sealed class BlackCard : Card
	{
		private int _pickCount = 1;

		[ClientFacing]
		[JsonProperty("pick", DefaultValueHandling = DefaultValueHandling.Populate)]
		[DefaultValue(1)]
		public int PickCount
		{
			get => _pickCount;
			private set
			{
				_pickCount = value < 1 ? 1 : value;
			}
		}

		[ClientFacing]
		[JsonProperty("draw", DefaultValueHandling = DefaultValueHandling.Populate)]
		public int DrawCount { get; set; }

		public override string ToString() => ID ?? "???";
	}
}
