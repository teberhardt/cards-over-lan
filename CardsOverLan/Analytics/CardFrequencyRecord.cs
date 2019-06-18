using LiteDB;

namespace CardsOverLan.Analytics
{
    internal sealed class CardFrequencyRecord
    {
        /*
         * Fields:
         *     CardId: ID field in database.
         *     Count: How many times a card is played in a single session.
         */
        [BsonId] public string CardId { get; set; } = "";
        [BsonField("count")] public int Count { get; set; }
    }
}