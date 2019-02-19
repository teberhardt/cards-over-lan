using Newtonsoft.Json;
using System.ComponentModel;
using System.Globalization;

namespace CardsOverLan.Game
{
	public sealed class BlackCard : Card
	{
		private int _pickCount = 1;

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

		public override string ToString() => GetContent(CultureInfo.CurrentCulture.IetfLanguageTag) ?? GetContent(DefaultLocale) ?? "???";
	}
}
