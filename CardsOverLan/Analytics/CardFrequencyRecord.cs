using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Analytics
{
	internal sealed class CardFrequencyRecord
	{
		[BsonId]
		public string CardId { get; set; } = "";
		[BsonField("count")]
		public int Count { get; set; } = 0;
	}
}
