using Ganss.XSS;

namespace CardsOverLan.Game
{
    internal static class StringUtilities
    {
        public static string SanitizeClientString(string rawClientString)
        {
            var doc = new HtmlSanitizer();

            return doc.Sanitize(rawClientString);
        }
    }
}