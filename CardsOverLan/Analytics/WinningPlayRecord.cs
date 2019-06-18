using LiteDB;

namespace CardsOverLan.Analytics
{
    internal sealed class WinningPlayRecord
    {
        /**
         * Fields:
         *     Id: Record ID.
         *     IsJudgeBot: True or false as to whether the judge is a bot.
         *     IsPlayerBot: True or false as to whether the winner of the round is a bot.
         *     BlackCard: Black card for the round.
         *     WhiteCards: White cards for the round.
         *     Count: TODO Possibly of number of people in game?
         */
        [BsonId] public ObjectId Id { get; set; }
        [BsonField("czar_is_bot")] public bool IsJudgeBot { get; set; }
        [BsonField("winner_is_bot")] public bool IsPlayerBot { get; set; }
        [BsonField("black_card")] public string BlackCard { get; set; }
        [BsonField("white_cards")] public string WhiteCards { get; set; }
        [BsonField("count")] public int Count { get; set; }
    }
}