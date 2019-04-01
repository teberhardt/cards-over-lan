using CardsOverLan.Analytics;
using CardsOverLan.Game.Trophies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game
{
	public delegate void PlayerCardsChangedEventDelegate(Player player, WhiteCard[] cards);
	public delegate void PlayerSelectionChangedEventDelegate(Player player, WhiteCard[] selection);
	public delegate void PlayerScoreChangedEventDelegate(Player player, int points);
	public delegate void PlayerNameChangedEventDelegate(Player player, string name);
	public delegate void PlayerJudgedCardsEventDelegate(Player player, int winningPlayIndex);
	public delegate void PlayerAfkChangedEventDelegate(Player player, bool afk);
	public delegate void PlayerAuxDataChangedEventDelegate(Player player);

	// TODO: Develop pattern for combining *Changed events to reduce unnecessary client updates
	public sealed class Player
	{
		private const string DefaultName = "Player";

		private readonly List<RoundPlay> _prevPlays;
		private readonly HashList<WhiteCard> _hand;
		private readonly HashList<WhiteCard> _selectedCards;
		private readonly HashList<Trophy> _trophies;
		private int _score;
		private int _coins;
		private int _discards;
		private string _name = DefaultName;
		private bool _afk;
		private bool _votedForSkip;
		private int _blankCardsRemaining;
		private readonly object _blankCardLock = new object();
		private readonly object _discardLock = new object();
		private readonly object _skipVoteLock = new object();
		private readonly object _botPlayDelayLock = new object();
		private readonly object _botJudgeDelayLock = new object();
		private uint _botPlayDelays = 0;
		private uint _botJudgeDelays = 0;
		private readonly Random _rng;

		public event PlayerCardsChangedEventDelegate CardsChanged;
		public event PlayerSelectionChangedEventDelegate SelectionChanged;
		public event PlayerScoreChangedEventDelegate ScoreChanged;
		public event PlayerNameChangedEventDelegate NameChanged;
		public event PlayerJudgedCardsEventDelegate JudgedCards;
		public event PlayerAfkChangedEventDelegate AfkChanged;
		public event PlayerAuxDataChangedEventDelegate AuxDataChanged;

		internal Player(CardGame game, int id, string token = "")
		{
			Game = game;
			Id = id;
			Token = token ?? "";
			_hand = new HashList<WhiteCard>();
			_selectedCards = new HashList<WhiteCard>();
			_prevPlays = new List<RoundPlay>();
			_trophies = new HashList<Trophy>();
			_rng = new Random(id);
		}

		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				RaiseNameChanged(value);
			}
		}

		public string Token { get; }

		public CardGame Game { get; }

		public int Id { get; }

		public int Score => _score;

		public int Coins => _coins;

		public int Discards
		{
			get
			{
				lock (_discardLock)
				{
					return _discards;
				}
			}
			set
			{
				lock (_discardLock)
				{
					_discards = value;
				}
			}
		}

		public bool IsAfk
		{
			get => _afk;
			set
			{
				if (_afk != value)
				{
					_afk = value;
					RaiseAfkChanged(value);
				}
			}
		}

		public bool IsAsshole { get; set; }

		public bool IsAutonomous { get; set; }

		public bool VotedForBlackCardSkip => _votedForSkip;

		public int RemainingBlankCards => _blankCardsRemaining;

		public void AddTrophy(Trophy trophy)
		{
			if (_trophies.Any(t =>
			!String.IsNullOrWhiteSpace(t.TrophyClass)
			&& String.Equals(t.TrophyClass, trophy.TrophyClass, StringComparison.InvariantCultureIgnoreCase)
			&& t.TrophyGrade > trophy.TrophyGrade))
			{
				return;
			}

			_trophies.RemoveAll(t =>
			!String.IsNullOrWhiteSpace(t.TrophyClass)
			&& String.Equals(t.TrophyClass, trophy.TrophyClass, StringComparison.InvariantCultureIgnoreCase)
			&& t.TrophyGrade < trophy.TrophyGrade);

			_trophies.Add(trophy);
		}

		public Trophy[] GetTrophies() => _trophies.ToArray();

		/// <summary>
		/// Adds cards to the player's hand. This does not remove cards from the game's draw pile.
		/// </summary>
		/// <param name="cards">The cards to add.</param>
		public void AddToHand(IEnumerable<WhiteCard> cards)
		{
			_hand.InsertRange(0, cards);
			RaiseCardsChanged();
		}

		/// <summary>
		/// Removes cards from the player's hand. This does not add cards to the game's discard pile.
		/// </summary>
		/// <param name="cards">The cards to remove.</param>
		public void RemoveFromHand(IEnumerable<WhiteCard> cards)
		{
			_hand.RemoveRange(cards);
			RaiseCardsChanged();
		}

		/// <summary>
		/// Enumerates the player's current hand.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<WhiteCard> GetCurrentHand()
		{
			foreach (var card in _hand.ToArray())
			{
				yield return card;
			}
		}

		public bool SetSkipVoteState(bool voted)
		{
			lock (_skipVoteLock)
			{
				if (!Game.Settings.AllowBlackCardSkips || Game.Stage != GameStage.RoundInProgress || IsAutonomous || voted == _votedForSkip) return false;
				_votedForSkip = voted;
				Game.UpdateSkipVotes();
				Console.WriteLine(voted ? $"{this} voted to skip black card" : $"{this} withdrew skip vote");
				return true;
			}
		}

		public void ClearSkipVote()
		{
			lock (_skipVoteLock)
			{
				_votedForSkip = false;
			}
		}

		/// <summary>
		/// Updates the player's selected cards to the specified cards and raises the <see cref="SelectionChanged"/> event.
		/// </summary>
		/// <param name="cards">The cards to select. They must be present in the hand at the time of calling.</param>
		/// <returns></returns>
		public bool PlayCards(IEnumerable<WhiteCard> cards)
		{
			if (!CanPlayCards) return false;
			var cardArray = cards.ToArray();

			// Count how many custom cards they are playing
			int numCustomCards = cards.Count(c => c.IsCustom);

			// Make sure they have enough custom cards for what they're requesting
			if (RemainingBlankCards < numCustomCards) return false;

			// Make sure they own all the cards they want to play, and that they are playing the correct number of cards
			if (cardArray.Length != Game.CurrentBlackCard.PickCount || cardArray.Any(c => !c.IsCustom && !HasWhiteCard(c))) return false;

			RemoveBlankCards(numCustomCards);
			_hand.RemoveRange(cards);
			_selectedCards.Clear();
			_selectedCards.AddRange(cardArray);

			// Record card usages
			RecordPlayedCards(cardArray);

			RaiseCardsChanged();
			RaiseSelectionChanged();
			return true;
		}

		private async void RecordPlayedCards(WhiteCard[] cards)
		{
			if (IsAutonomous) return;
			await Task.Run(() =>
			{
				foreach (var card in cards)
				{
					if (card.IsCustom) continue;
					AnalyticsManager.Instance.RecordCardUseAsync(card);
				}
			});
		}

		public bool UpgradeCard(WhiteCard card)
		{
			if (!Game.Settings.UpgradesEnabled || card == null || !HasWhiteCard(card)) return false;
			var tierCard = Game.GetNextTierCard(card);
			if (tierCard == null) return false;
			if (!SpendCoins(tierCard.TierCost)) return false;
			_hand.Replace(card, tierCard);
			RaiseCardsChanged();
			Console.WriteLine($"{this} upgraded {card.ID} to {tierCard.ID} (-{tierCard.TierCost} CC)");
			return true;
		}

		public bool DiscardCard(WhiteCard card)
		{
			if (card == null || !HasWhiteCard(card)) return false;
			if (!SpendDiscard()) return false;
			_hand.Remove(card);
			Game.Deal(this);
			RaiseCardsChanged();
			AnalyticsManager.Instance.RecordDiscardAsync(card);
			return true;
		}

		public async void AutoPlayAsync()
		{
			if (!IsAutonomous) return;
			lock (_botPlayDelayLock)
			{
				_botPlayDelays++;
			}
			int pickCount = Game.CurrentBlackCard.DrawCount;
			var cfg = Game.Settings.BotConfig;
			int delayBase = _rng.Next(cfg.PlayMinBaseDelay, cfg.PlayMaxBaseDelay + 1);
			int delayCards = _rng.Next(cfg.PlayMinPerCardDelay * pickCount, cfg.PlayMaxPerCardDelay * pickCount + 1);
			await Task.Delay(delayBase + delayCards);
			lock (_botPlayDelayLock)
			{
				_botPlayDelays--;
				if (_botPlayDelays == 0)
				{
					PlayCards(GetCurrentHand().Take(Game.CurrentBlackCard.PickCount));
				}
			}
		}

		public async void AutoJudgeAsync()
		{
			if (!IsAutonomous) return;
			lock (_botJudgeDelayLock)
			{
				_botJudgeDelays++;
			}
			int playCount = Game.GetRoundPlays().Count();
			int pickCount = Game.CurrentBlackCard.DrawCount;
			var cfg = Game.Settings.BotConfig;
			int delayCards = _rng.Next(cfg.JudgeMinPerCardDelay * pickCount, cfg.JudgeMaxPerCardDelay * pickCount + 1);
			int delayPlays = _rng.Next(cfg.JudgeMinPerPlayDelay * playCount, cfg.JudgeMaxPerPlayDelay * playCount + 1);
			await Task.Delay(delayCards + delayPlays);
			lock (_botJudgeDelayLock)
			{
				_botJudgeDelays--;
				if (_botJudgeDelays == 0)
				{					
					JudgeCards(_rng.Next(playCount));
				}
			}
		}

		public bool JudgeCards(int winningPlayIndex)
		{
			if (!CanJudgeCards) return false;
			RaiseJudgedCards(winningPlayIndex);
			return true;
		}

		public void AddBlankCards(int numBlankCards)
		{
			lock (_blankCardLock)
			{
				if (numBlankCards <= 0) return;
				_blankCardsRemaining += numBlankCards;
				RaiseCardsChanged();
			}
		}

		public void RemoveBlankCards(int numBlankCards)
		{
			lock (_blankCardLock)
			{
				if (numBlankCards == 0 || _blankCardsRemaining < numBlankCards) return;
				_blankCardsRemaining -= numBlankCards;
				RaiseCardsChanged();
			}
		}

		public void SetBlankCards(int numBlankCards)
		{
			lock(_blankCardLock)
			{
				if (numBlankCards < 0) return;
				_blankCardsRemaining = numBlankCards;
				RaiseCardsChanged();
			}
		}

		internal void SaveCurrentPlay(bool winning)
		{
			var play = new RoundPlay(this, _selectedCards, Game.CurrentBlackCard)
			{
				Winning = winning
			};
			_prevPlays.Add(play);
		}

		public IEnumerable<RoundPlay> GetPreviousPlays()
		{
			foreach (var play in _prevPlays)
			{
				yield return play;
			}
		}

		public void ResetAwards()
		{
			_score = 0;
			_coins = 0;
			_trophies.Clear();
			RaiseAuxDataChanged();
		}

		public bool SpendCoins(int coins)
		{
			if (coins > _coins) return false;
			_coins -= coins;
			RaiseAuxDataChanged();
			return true;
		}

		public bool SpendDiscard()
		{
			lock (_discardLock)
			{
				if (_discards <= 0) return false;
				_discards--;
				return true;
			}
		}

		public void ClearPreviousPlays()
		{
			_prevPlays.Clear();
		}

		/// <summary>
		/// Enumerates the player's currently selected cards.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<WhiteCard> GetSelectedCards() => _selectedCards.ToArray();

		/// <summary>
		/// Indicates whether the player's current selection is valid for the game's current black card.
		/// </summary>
		public bool IsSelectionValid => Game.CurrentBlackCard.PickCount > 0 && _selectedCards.Count > 0 && _selectedCards.Count == Game.CurrentBlackCard.PickCount;

		/// <summary>
		/// Indicates whether the player has the specified white card.
		/// </summary>
		/// <param name="card">The card to look for.</param>
		/// <returns></returns>
		public bool HasWhiteCard(WhiteCard card) => _hand.Contains(card);

		/// <summary>
		/// Indicates whether the player is currently allowed to play cards.
		/// </summary>
		public bool CanPlayCards => Game.Stage == GameStage.RoundInProgress && _selectedCards.Count == 0 && Game.Judge != this;

		public bool CanJudgeCards => Game.Stage == GameStage.JudgingCards && Game.Judge == this;

		/// <summary>
		/// Adds points/upgrade points to the player's score.
		/// </summary>
		/// <param name="points">The number of points to add. Use a negative number to remove points.</param>
		public void AddPoints(int points)
		{
			_score += points;
			if (points != 0)
			{
				RaiseScoreChanged();
			}
		}

		public void AddAuxPoints(int auxPoints)
		{
			_coins += auxPoints > 0 ? auxPoints : 0;
			RaiseAuxDataChanged();
		}

		/// <summary>
		/// Dumps the player's cards to the game discard pile.
		/// </summary>
		public void DiscardHand()
		{
			Game.MoveToDiscardPile(_hand);
			RaiseCardsChanged();
		}

		/// <summary>
		/// Moves the player's selected cards to the discard pile.
		/// </summary>
		public void DiscardSelection()
		{
			Game.MoveToDiscardPile(_selectedCards);
			RaiseSelectionChanged();
		}

		private void RaiseCardsChanged()
		{
			CardsChanged?.Invoke(this, _hand.ToArray());
		}

		private void RaiseScoreChanged()
		{
			ScoreChanged?.Invoke(this, _score);
		}

		private void RaiseSelectionChanged()
		{
			SelectionChanged?.Invoke(this, _selectedCards.ToArray());
		}

		private void RaiseJudgedCards(int winIndex)
		{
			JudgedCards?.Invoke(this, winIndex);
		}

		private void RaiseNameChanged(string name)
		{
			NameChanged?.Invoke(this, name);
		}

		private void RaiseAfkChanged(bool afk)
		{
			AfkChanged?.Invoke(this, afk);
		}

		private void RaiseAuxDataChanged()
		{
			AuxDataChanged?.Invoke(this);
		}

		public int HandSize => _hand.Count;

		public override string ToString() => $"{Name} (#{Id})";

		public override int GetHashCode() => Token.GetHashCode() ^ Id.GetHashCode();
	}
}
