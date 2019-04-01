using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Analytics
{
	internal sealed class WinningPlayRecord
	{
		[BsonId]
		public ObjectId Id { get; set; }
		[BsonField("czar_is_bot")]
		public bool IsJudgeBot { get; set; }
		[BsonField("winner_is_bot")]
		public bool IsPlayerBot { get; set; }
		[BsonField("black_card")]
		public string BlackCard { get; set; }
		[BsonField("white_cards")]
		public string WhiteCards { get; set; }
		[BsonField("count")]
		public int Count { get; set; }
	}
}
