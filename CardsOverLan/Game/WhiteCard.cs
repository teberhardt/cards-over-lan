using System.Globalization;

namespace CardsOverLan.Game
{
	public sealed class WhiteCard : Card
    {

        public override string ToString() => GetContent(CultureInfo.CurrentCulture.IetfLanguageTag) ?? GetContent(DefaultLocale) ?? "???";
    }
}
