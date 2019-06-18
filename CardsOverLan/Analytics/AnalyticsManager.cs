using CardsOverLan.Game;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CardsOverLan.Analytics
{
	public sealed class AnalyticsManager
	{
		public static AnalyticsManager Instance { get; } = new AnalyticsManager();

		private const string CardsPlayedTableName = "cards_played";
		private const string DiscardsTableName = "cards_discarded";
		private const string WinningPlaysTableName = "winning_plays";
		private const string SkippedBlackCardsTableName = "skipped_black_cards";
		private const string ReferrersTableName = "referrers";

		private LiteDatabase _db;
		private readonly object _dbLock = new object();

		public bool IsActive { get; private set; }

		public void Start(string databaseFile)
		{
			lock (_dbLock)
			{
				if (IsActive) return;
				try
				{
					// Check directory first
					var parentDir = Path.GetDirectoryName(databaseFile);
					if (!Directory.Exists(parentDir))
					{
						Directory.CreateDirectory(parentDir);
					}

					// Load/create DB
					_db = new LiteDatabase(databaseFile);
					IsActive = true;

					// Subscribe (and smash that like button)
					var game = GameManager.Instance.Game;
					game.RoundEnded += OnGameRoundEnded;
					game.BlackCardSkipped += OnGameBlackCardSkipped;

					Console.WriteLine("Analytics enabled.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to load database: {ex.Message}");
				}
			}
		}

		private void OnGameBlackCardSkipped(BlackCard skippedCard, BlackCard replacementCard)
		{
			RecordBlackCardSkip(skippedCard);
		}

		private void OnGameRoundEnded(int round, BlackCard blackCard, Player roundJudge, Player roundWinner, WhiteCard[] winningPlay)
		{
			RecordWinningPlay(blackCard, winningPlay, roundJudge.IsAutonomous, roundWinner.IsAutonomous);
		}

		public async void RecordReferrer(string referrer)
		{
			if (!IsActive) return;
			await Task.Run(() =>
			{
				lock(_dbLock)
				{
					try
					{	
						var table = _db.GetCollection<StringFrequencyRecord>(ReferrersTableName);
						var record = table.FindById(referrer);
						if (record == null)
						{
							record = new StringFrequencyRecord
							{
								Value = referrer,
								Count = 0
							};
							table.Insert(record);
						}
						record.Count++;
						table.Update(record);

						table.EnsureIndex(r => r.Value);
					}
					catch(Exception ex)
					{
						Console.WriteLine($"Failed to record referrer in DB: {ex.Message}");
					}
				}
			});
		}

		public async void RecordBlackCardSkip(BlackCard card)
		{
			if (!IsActive) return;
			await Task.Run(() =>
			{
				lock(_dbLock)
				{
					try
					{
						var table = _db.GetCollection<CardFrequencyRecord>(SkippedBlackCardsTableName);
						var record = table.FindById(card.Id);
						if (record == null)
						{
							record = new CardFrequencyRecord
							{
								CardId = card.Id,
								Count = 0
							};
							table.Insert(record);
						}
						record.Count++;
						table.Update(record);

						table.EnsureIndex(r => r.CardId);
					}
					catch(Exception ex)
					{
						Console.WriteLine($"Failed to record skipped black card in DB: {ex.Message}");
					}
				}
			});
		}

		public async void RecordCardUseAsync(WhiteCard card)
		{
			if (!IsActive) return;
			await Task.Run(() => {
				lock (_dbLock)
				{
					try
					{
						var cardId = card.IsCustom ? "custom" : card.Id;
						var table = _db.GetCollection<CardFrequencyRecord>(CardsPlayedTableName);
						var record = table.FindById(cardId);
						if (record == null)
						{
							record = new CardFrequencyRecord
							{
								CardId = cardId,
								Count = 0
							};
							table.Insert(record);
						}
						record.Count++;
						table.Update(record);

						table.EnsureIndex(r => r.CardId);
					}
					catch(Exception ex)
					{
						Console.WriteLine($"Failed to record card usage in DB: {ex.Message}");
					}
				}
			});
		}

		public async void RecordDiscardAsync(WhiteCard card)
		{
			if (!IsActive) return;
			await Task.Run(() =>
			{
				lock(_dbLock)
				{
					try
					{
						var table = _db.GetCollection<CardFrequencyRecord>(DiscardsTableName);
						var record = table.FindById(card.Id);
						if (record == null)
						{
							record = new CardFrequencyRecord
							{
								CardId = card.Id,
								Count = 0
							};
							table.Insert(record);
						}
						record.Count++;
						table.Update(record);

						table.EnsureIndex(r => r.CardId);
					}
					catch(Exception ex)
					{
						Console.WriteLine($"Failed to record discard in DB: {ex.Message}");
					}
				}
			});
		}

		public async void RecordWinningPlay(BlackCard blackCard, IEnumerable<WhiteCard> whiteCards, bool isWinnerBot, bool isJudgeBot)
		{
			if (!IsActive) return;
			await Task.Run(() =>
			{
				lock(_dbLock)
				{
					try
					{
						var whiteCardListString = string.Join(";", whiteCards.Select(w => w.IsCustom ? "custom" : w.Id));
						var table = _db.GetCollection<WinningPlayRecord>(WinningPlaysTableName);
						var record = table.FindOne(r =>
						r.IsPlayerBot == isWinnerBot
						&& r.IsJudgeBot == isJudgeBot
						&& r.BlackCard == blackCard.Id
						&& r.WhiteCards == whiteCardListString);

						if (record == null)
						{
							record = new WinningPlayRecord
							{
								Id = ObjectId.NewObjectId(),
								IsJudgeBot = isJudgeBot,
								IsPlayerBot = isWinnerBot,
								BlackCard = blackCard.Id,
								WhiteCards = whiteCardListString,
								Count = 0
							};
							table.Insert(record);
						}
						record.Count++;
						table.Update(record);

						table.EnsureIndex(r => r.BlackCard);
						table.EnsureIndex(r => r.WhiteCards);
						table.EnsureIndex(r => r.IsJudgeBot);
						table.EnsureIndex(r => r.IsPlayerBot);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Failed to record winning play in DB: {ex.Message}");
					}
				}
			});
		}

		public void Stop()
		{
			lock(_dbLock)
			{
				if (!IsActive) return;
				_db.Dispose();
				IsActive = false;
			}
		}
	}
}
