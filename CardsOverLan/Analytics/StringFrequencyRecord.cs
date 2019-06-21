using LiteDB;

namespace CardsOverLan.Analytics
{
    internal sealed class StringFrequencyRecord
    {
        /**
         * TODO This class is apparently for referrers. No clue what that means.
         */
        [BsonId] public string Value { get; set; }
        public int Count { get; set; }
    }
}