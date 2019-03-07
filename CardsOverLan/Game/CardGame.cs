using HtmlAgilityPack;
using CardsOverLan.Game.Trophies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CardsOverLan.Game
{
	public delegate void PlayerJoinedEventDelegate(Player player);
	public delegate void PlayerLeftEventDelegate(Player player, string reason);
	public delegate void PlayersChangedEventDelegate();
	public delegate void GameStateChangedEventDelegate();
	public delegate void RoundStartedEventDelegate();
	public delegate void GameStageChangedEventDelegate(in GameStage oldStage, in GameStage currentStage);
	public delegate void RoundEndedEventDelegate(int round, Player roundWinner);
	public delegate void GameEndedEventDelegate(Player[] winners);
    public delegate void BlackCardSkippedEventDelegate(BlackCard skippedCard, BlackCard replacementCard);

	// TODO: Find some way to combine state updates
	public sealed class CardGame
	{
		#region Constants
		private const string DefaultPlayerName = "Player";
		private const string DefaultBotName = "Bot";

		// Shuffle iterations
		private const int ShufflePasses = 100;

		private static readonly HashSet<char> PlayerNameCharExceptions = new HashSet<char>(new[] { ' ', '-', '_', '\'', '\"', '\u00ae', '\u2122', '.', ',' });
		#endregion

		// Index of the current black card being used
		private int _blackCardIndex;

		// ID for next player created
		private int _nextPlayerId = 0;

		// White cards drawn by players, also used for dealing
		private readonly HashList<WhiteCard> _whiteDrawPile;

		// White cards discarded from previous players
		private readonly HashList<WhiteCard> _whiteDiscardPile;

		// All black cards in the game
		private readonly HashList<BlackCard> _blackCards;

		// All white cards in the game
		private readonly HashList<WhiteCard> _whiteCards;

        // All trophies in the game
        private readonly HashList<Trophy> _trophies;

		// All packs in the game
		private readonly Pack[] _packs;

		// All cards in the game
		private readonly Dictionary<string, Card> _cards;

		// RNG
		private readonly Random _rng;

		// Current players in the game
		private readonly HashList<Player> _players;

		// Current stage of the game
		private GameStage _stage = GameStage.GameStarting;

		// Current round judge
		private int _judgeIndex = -1;

		// Cards played for round. Only populated once judging has begun
		private readonly HashList<(Player, WhiteCard[])> _roundPlays;

		// Index of winning play
		private int _winningPlayIndex = -1;

		// Round number
		private int _roundNum = 0;

		private readonly object _allPlayersSync = new object();
		private readonly object _stageChangeLock = new object();
        private readonly object _skipCheckLock = new object();

		// Raised when player joins game
		public event PlayerJoinedEventDelegate PlayerJoined;
		// Raised when player leaves game
		public event PlayerLeftEventDelegate PlayerLeft;
		// Raised when game state is updated
		public event GameStateChangedEventDelegate GameStateChanged;
		// Raised when a player has joined, left, or been modified
		public event PlayersChangedEventDelegate PlayersChanged;
		// Raised when a new round has started
		public event RoundStartedEventDelegate RoundStarted;
		// Raised when game stage has changed
		public event GameStageChangedEventDelegate StageChanged;
		// Raised when a round has ended
		public event RoundEndedEventDelegate RoundEnded;
        // Raised when game has ended
        public event GameEndedEventDelegate GameEnded;
        // Raised when a black card is skipped
        public event BlackCardSkippedEventDelegate BlackCardSkipped;

		public CardGame(IEnumerable<Pack> packs, GameSettings settings)
		{
			Settings = settings;
			_whiteDrawPile = new HashList<WhiteCard>();
			_whiteDiscardPile = new HashList<WhiteCard>();
			_blackCards = new HashList<BlackCard>();
			_whiteCards = new HashList<WhiteCard>();
			_players = new HashList<Player>();
			_rng = new Random();
			_cards = new Dictionary<string, Card>();
			_roundPlays = new HashList<(Player, WhiteCard[])>();
            _trophies = new HashList<Trophy>();

			_packs = packs
                .Where(p => Settings.UsePacks == null || Settings.UsePacks.Length == 0 || Settings.UsePacks.Contains(p.Id, StringComparer.InvariantCultureIgnoreCase))
                .ToArray();

			// Combine decks and remove duplicates
			foreach (var card in _packs                
				.SelectMany(d => d.GetAllCards())
				.Where(c => settings.RequiredLanguages?.Length == 0 || settings.RequiredLanguages.All(l => c.SupportsLanguage(l))))
			{
				if (!_cards.ContainsKey(card.ID))
				{
					_cards.Add(card.ID, card);
				}
				else
				{
					Console.WriteLine($"Duplicate card ID: {card.ID} in [{card.Owner.Name}]");
				}
			}

			_blackCards.AddRange(_cards.Values.OfType<BlackCard>().Where(c => !Settings.ContentExclusions.Any(x => c.ContainsContentFlags(x))));
			_whiteCards.AddRange(_cards.Values.OfType<WhiteCard>()
                .Where(c => !Settings.ContentExclusions.Any(x => c.ContainsContentFlags(x)))
                .Where(c => Settings.UpgradesEnabled ? c.Tier <= 0 : String.IsNullOrEmpty(c.NextTierId)));

            // Combine trophies
            foreach(var trophy in packs.SelectMany(p => p.GetTrophies()))
            {
                _trophies.Add(trophy);
            }

			ResetCards();
			NextJudge();

			// Init bots
			if (settings.BotCount > 0)
			{
				for (int i = 0; i < settings.BotCount; i++)
				{
					var bot = CreatePlayer(settings.BotNames != null && settings.BotNames.Length > 0 ? settings.BotNames[i % settings.BotNames.Length] : DefaultBotName, true);
				}
			}
		}

		/// <summary>
		/// Resets piles and re-deals to players.
		/// </summary>
		private void ResetCards()
		{
			lock (_allPlayersSync)
			{
				var playerArray = _players.ToArray();

				// Take everyone's cards away
				foreach (var player in playerArray)
				{
					player.DiscardHand();
					player.DiscardSelection();
					player.ClearPreviousPlays();
				}

				// Reset the draw pile and shuffle it
				_whiteDrawPile.Clear();
				_whiteDrawPile.AddRange(_whiteCards);
				Shuffle(_whiteDrawPile);

				// Shuffle the black cards to keep it fresh
				Shuffle(_blackCards);

				// Reset the discard pile and black card index
				_whiteDiscardPile.Clear();
				_blackCardIndex = 0;

				// Deal cards out again
				foreach (var player in playerArray)
				{
					if (!_players.Contains(player)) continue;
					Deal(player);
				}
			}
		}

		private void NewRound()
		{
			lock (_allPlayersSync)
			{
                // Move to next black card
                NextBlackCard();

				// Reset player selections
				foreach (var player in _players)
				{
					player.DiscardSelection();
					Deal(player, CurrentBlackCard.DrawCount);
				}

				// Move to next judge
				NextJudge();

				// Move to next round
				_roundNum++;

				// Change stage
				Stage = GameStage.RoundInProgress;

				// Raise round started event
				RaiseRoundStarted();
			}
		}

		private void EndGame()
		{
			lock (_allPlayersSync)
			{
				Stage = GameStage.GameEnd;
				GameEndTimeoutAsync();
			}
		}

        private void NextBlackCard()
        {   
            if (_blackCardIndex >= _blackCards.Count - 1)
            {
                Shuffle(_blackCards);
                _blackCardIndex = 0;
            }
            else
            {
                _blackCardIndex = (_blackCardIndex + 1) % _blackCards.Count;
            }
        }

		private void NextJudge()
		{
			lock (_allPlayersSync)
			{
				var judge = Judge;
				int n = PlayerCount;
				var assholes = _players.Select((p, i) => (index: i, player: p)).Where(t => t.player.IsAsshole).ToArray();

				// If nobody's home, it's simple; nobody is the judge. Goodbye.
				if (n == 0)
				{
					_judgeIndex = -1;
					return;
				}

				if (Settings.PermanentCzar)
				{
					// If no judge or the judge is AFK, pick a new one
					if (_judgeIndex < 0 || (judge != null && judge.IsAfk))
					{
						// Start at a random point in the player list and linear search until a non-AFK player is found
						int offset = _rng.Next(n);
						for (int i = 0; i < n; i++)
						{
							int index = (i + offset) % n;
							var judgeCandidate = _players[index];
							// Ignore them if they're AFK
							if (judgeCandidate.IsAfk || (!Settings.AllowBotCzars && judgeCandidate.IsAutonomous)) continue;
							_judgeIndex = index;
							return;
						}
						// If everyone's AFK, just make a random person judge, oh well.
						_judgeIndex = _rng.Next(n);
					}
				}
				else
				{
					// Let's see if someone needs to be punished.
					if (assholes.Length > 0 && _rng.Next(0, 2) == 0)
					{
						int offset = _rng.Next(assholes.Length);
						for (int i = 0; i < assholes.Length; i++)
						{
							int index = (offset + i) % assholes.Length;
							// Ignore AFK assholes
							if (assholes[index].player.IsAfk) continue;
							_judgeIndex = assholes[index].index;
							return;
						}
					}

					// If winner_czar is enabled, choose the round winner
					if (Settings.WinnerCzar && _winningPlayIndex > -1 && (Settings.AllowBotCzars || !RoundWinner.IsAutonomous))
					{
						_judgeIndex = _players.IndexOf(RoundWinner);
						return;
					}

                    if (_judgeIndex < 0) _judgeIndex = 0;

					// Make the next non-AFK person the judge.
					for (int i = 1; i < n - 1; i++)
					{
						int index = (_judgeIndex + i) % n;
						var judgeCandidate = _players[index];
						if (judgeCandidate.IsAfk || (!Settings.AllowBotCzars && judgeCandidate.IsAutonomous)) continue;
						_judgeIndex = index;
						return;
					}

					// Fallback
					_judgeIndex = PlayerCount > 0 ? (_judgeIndex + 1) % n : -1;
				}
			}
		}

		private void NewGame()
		{
            lock(_allPlayersSync)
            {
                // Reset scores
                foreach(var p in _players)
                {
					p.Discards = Settings.Discards;
                    p.ResetAwards();
                }
                
                // Clear play data
			    ClearRoundPlays();
                // Reset all cards
			    ResetCards();
                // Reset round
			    _roundNum = 0;
			    _judgeIndex = -1;

			    Stage = GameStage.GameStarting;
                RaisePlayersChanged();
            }
		}

		private void Shuffle<T>(HashList<T> list)
		{
			int n = list.Count;
			for (int i = 0; i < ShufflePasses; i++)
			{
				for (int j = 0; j < n; j++)
				{
					int s = (_rng.Next(n - 1) + j + 1) % n;
					list.Swap(j, s);
				}
			}
		}

		/// <summary>
		/// Enumerates all players in the game.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Player> GetPlayers()
		{
			lock (_allPlayersSync)
			{
				return _players.ToArray();
			}
		}

		/// <summary>
		/// Enumerates all packs used in the game.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Pack> GetPacks()
		{
			foreach(var p in _packs)
			{
				yield return p;
			}
		}

		/// <summary>
		/// Enumerates the current top player(s) in the game.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Player> GetWinningPlayers()
		{
			lock (_allPlayersSync)
			{
				int maxScore = _players.Max(p => p.Score);
				foreach (var player in _players.Where(p => p.Score == maxScore))
				{
					yield return player;
				}
			}
		}

		/// <summary>
		/// Enumerates all players that still need to play cards.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Player> GetPendingPlayers()
		{
			lock (_allPlayersSync)
			{
				if (Stage != GameStage.RoundInProgress) yield break;
				bool activeRound = false;
				foreach (var player in _players.ToArray())
				{
					if (!player.IsSelectionValid && Judge != player)
					{
						yield return player;
					}
				}
			}
		}

		/// <summary>
		/// Enumerates played cards for current round.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<(Player, WhiteCard[])> GetRoundPlays()
		{
			lock (_allPlayersSync)
			{
				foreach (var play in _roundPlays)
				{
					yield return play;
				}
			}
		}

		public IEnumerable<WhiteCard> DrawWhiteCards(int count)
		{
			for (int i = 0; i < count; i++)
			{
				// TODO: Warn admin if not enough cards for players
				if (_whiteDrawPile.Count == 0)
				{
					_whiteDrawPile.AddRange(_whiteDiscardPile);
					_whiteDiscardPile.Clear();
					Shuffle(_whiteDrawPile);
					if (_whiteDrawPile.Count == 0) yield break;
				}

				int cardIndex = _whiteDrawPile.Count - 1;
				var card = _whiteDrawPile[cardIndex];
				_whiteDrawPile.RemoveAt(cardIndex);
				yield return card;
			}
		}

		/// <summary>
		/// Ensures that the player has a full hand, plus an optional number of additional cards.
		/// </summary>
		/// <param name="player">The player to deal to.</param>
		/// <param name="extraCards">(Optional) The number of additional cards to deal.</param>
		internal void Deal(Player player, int extraCards = 0)
		{
			int drawNum = Math.Max(0, Settings.HandSize - player.HandSize + extraCards);
			if (drawNum <= 0) return;
			player.AddToHand(DrawWhiteCards(drawNum));
		}

		/// <summary>
		/// Creates and returns a new player ID.
		/// </summary>
		/// <returns></returns>
		private int CreatePlayerId()
		{
			int id = _nextPlayerId;
			_nextPlayerId = (_nextPlayerId + 1) % int.MaxValue;
			return id;
		}

		/// <summary>
		/// (Stage = <see cref="GameStage.RoundInProgress"/>): Checks if all players have played, and transitions to judging phase if so.
		/// </summary>
		private void CheckRoundPlays()
		{
			lock (_allPlayersSync)
			{
				if (Stage != GameStage.RoundInProgress || _players.All(p => p.IsAfk)) return;
				if (_players.All(p => p.IsSelectionValid || Judge == p || p.IsAfk) && !_players.All(p => p.IsAfk || p == Judge))
				{
					Stage = GameStage.JudgingCards;
				}
			}
		}

		public Card GetCardById(string id)
		{
			if (String.IsNullOrWhiteSpace(id)) return null;
			return _cards.TryGetValue(id, out var card) ? card : null;
		}

		public string CreatePlayerName(string requestedName, Player player)
		{
			var namePreSan = requestedName?.Trim() ?? DefaultPlayerName;
			bool asshole = false;
			var sanitizedName = StringUtilities.SanitizeClientString(
				namePreSan,
				Settings.MaxPlayerNameLength,
				ref asshole,
				c => !Char.IsControl(c) && (Char.IsLetterOrDigit(c) || PlayerNameCharExceptions.Contains(c)));

			player.IsAsshole |= asshole;

			if (sanitizedName.Length == 0) sanitizedName = DefaultPlayerName;

			var currentName = sanitizedName;
			int iter = 2;
			while (PlayerNameExists(currentName))
			{
				currentName = $"{sanitizedName} {iter}";
				iter++;
			}
			return currentName;
		}

		private bool PlayerNameExists(string name) => _players.Any(p => p.Name == name);

		/// <summary>
		/// Creates a new play and adds them to the game.
		/// </summary>
		/// <param name="name">The requested player name.</param>
		/// <param name="bot">Are they a bot?</param>
		/// <returns></returns>
		public Player CreatePlayer(string name = null, bool bot = false)
		{
			lock (_allPlayersSync)
			{
				var player = new Player(this, CreatePlayerId());
				player.Name = CreatePlayerName(name, player);
				if (bot) player.IsAutonomous = true;

				// Subscribe events
				player.SelectionChanged += OnPlayerSelectionChanged;
				player.NameChanged += OnPlayerNameChanged;
				player.JudgedCards += OnPlayerJudgedCards;
				player.ScoreChanged += OnPlayerScoreChanged;
				player.AfkChanged += OnPlayerAfkChanged;

				// Give them some cards
				Deal(player, CurrentBlackCard?.DrawCount ?? 0);
				player.AddBlankCards(Settings.BlankCards);
				player.Discards = Settings.Discards;

				// Add them to the player list
				_players.Add(player);
				RaisePlayerJoined(player);
				return player;
			}
		}

		/// <summary>
		/// Removes a player from the game.
		/// </summary>
		/// <param name="player">The player to remove.</param>
		/// <param name="reason">The reason for removal.</param>
		/// <returns></returns>
		public bool RemovePlayer(Player player, string reason)
		{
			lock (_allPlayersSync)
			{
                if (player == null || !_players.Remove(player)) return false;                

				// Unsubscribe events
				player.SelectionChanged -= OnPlayerSelectionChanged;
				player.NameChanged -= OnPlayerNameChanged;
				player.JudgedCards -= OnPlayerJudgedCards;
				player.ScoreChanged -= OnPlayerScoreChanged;
				player.AfkChanged -= OnPlayerAfkChanged;

				// Reclaim their cards
				player.DiscardHand();
				player.DiscardSelection();

				if (Judge == player)
				{
					NextJudge();
				}

				RaisePlayerLeft(player, reason);
				return true;
			}
		}

        internal void UpdateSkipVotes()
        {
            lock(_allPlayersSync)              
            {
                lock(_skipCheckLock)
                {
                    if (Stage != GameStage.RoundInProgress) return;
                    var cardToSkip = CurrentBlackCard;
                    int eligibleSkipVoterCount = _players.Count(p => !p.IsAutonomous && !p.IsAfk && !p.IsAsshole);
                    int numVoted = _players.Count(p => p.VotedForBlackCardSkip);

                    if (numVoted > 0)
                    {
                        if (eligibleSkipVoterCount == 0 || numVoted * 100 / eligibleSkipVoterCount > 50)
                        {
                            foreach (var p in _players)
                            {
                                p.DiscardSelection();
                            }
                            NextBlackCard();
                            RaiseStateChanged();
                            RaiseBlackCardSkipped(cardToSkip, CurrentBlackCard);
                            ClearSkipVotes();
                            PromptAutoPlays();
                        }
                    }

                    RaisePlayersChanged();
                }
            }
        }

        public void ClearSkipVotes()
        {
            lock(_allPlayersSync)
            {
                foreach(var player in _players)
                {
                    player.ClearBlackCardSkipVote();
                }
                RaisePlayersChanged();
            }
        }

		/// <summary>
		/// Clears the plays for the round and resets the winning play index.
		/// </summary>
		private void ClearRoundPlays()
		{
			_roundPlays.Clear();
			_winningPlayIndex = -1;
		}

		private void PopulateRoundPlays()
		{
			lock (_allPlayersSync)
			{
				_roundPlays.Clear();
				_roundPlays.AddRange(_players.Where(p => p != Judge && p.IsSelectionValid).Select(p => (p, p.GetSelectedCards().ToArray())));
				Shuffle(_roundPlays); // mitigate favoritism
			}
		}

		private void CheckMinPlayers()
		{
			if (PlayerCount < Settings.MinPlayers)
			{
				NewGame();
			}
		}

		/// <summary>
		/// Enumerates all black cards.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<BlackCard> GetBlackCards()
		{
			foreach (var card in _blackCards) yield return card;
		}

		/// <summary>
		/// Enumerates all white cards.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<WhiteCard> GetWhiteCards()
		{
			foreach (var card in _whiteCards) yield return card;
		}

        public WhiteCard GetNextTierCard(WhiteCard card)
        {
            return String.IsNullOrWhiteSpace(card.NextTierId) ? null : _cards.TryGetValue(card.NextTierId, out var tierCard) ? tierCard as WhiteCard : null;
        }

		/// <summary>
		/// Moves all the contents of the provided hashlist of white cards to the disacard pile.
		/// </summary>
		/// <param name="cards">The cards to discard.</param>
		public void MoveToDiscardPile(HashList<WhiteCard> cards)
		{
			cards.MoveTo(_whiteDiscardPile);
		}

		private void PromptAutoPlays()
		{
			lock(_allPlayersSync)
			{
				if (_players.Any(p => !p.IsAutonomous))
				{
					foreach(var p in _players.Where(p => p.IsAutonomous))
					{
						p.AutoPlayAsync();
					}
				}
			}
		}

		private void PromptAutoJudge()
		{
			lock (_allPlayersSync)
			{
				if (Judge?.IsAutonomous ?? false)
				{
					Judge.AutoJudgeAsync();
				}
			}
		}

		private void PromptBots()
		{
			switch (Stage)
			{
				case GameStage.RoundInProgress:
					PromptAutoPlays();
					break;
				case GameStage.JudgingCards:
					PromptAutoJudge();
					break;
			}
		}

		#region Properties

		/// <summary>
		/// The current stage of the game.
		/// </summary>
		public GameStage Stage
		{
			get => _stage;
			set
			{
				lock (_stageChangeLock)
				{
					var old = _stage;
					_stage = value;
					OnStageChanged(old, value);
				}
			}
		}

		public int BlackCardCount => _blackCards.Count;

		public int WhiteCardCount => _whiteCards.Count;

		public int Round => _roundNum;

		public int PlayerCount => _players.Count;

		public int WinningPlayIndex => _winningPlayIndex;

		public Player RoundWinner => _winningPlayIndex < 0 || _winningPlayIndex >= _roundPlays.Count ? null : _roundPlays[_winningPlayIndex].Item1;

		public BlackCard CurrentBlackCard => _blackCardIndex >= 0 && _blackCardIndex < _blackCards.Count ? _blackCards[_blackCardIndex] : null;

		public Player Judge => PlayerCount == 0 || _judgeIndex < 0 || _judgeIndex >= _players.Count ? null : _players[_judgeIndex];

		public GameSettings Settings { get; }

		#endregion

		#region Event Handlers

		private void OnPlayerSelectionChanged(Player player, WhiteCard[] selection)
		{
			lock (_allPlayersSync)
			{
				if (Stage == GameStage.RoundInProgress && player.IsSelectionValid)
				{
					Console.WriteLine($"{player} selected: {selection.Select(c => c.IsCustom ? $"(Custom) {c.GetContent("en-US")}" : c.ToString()).Aggregate((c, n) => $"{c}, {n}")}");
					CheckRoundPlays();
					Deal(player);
					RaiseStateChanged();
				}
			}
		}

		// Called when a player joins or leaves.
		private void OnPlayerCountChanged()
		{
			switch (Stage)
			{
				case GameStage.GameStarting:
				{
					if (PlayerCount >= Settings.MinPlayers)
					{
						NewRound();
					}
					break;
				}
				case GameStage.RoundInProgress:
				case GameStage.JudgingCards:
				case GameStage.RoundEnd:
				case GameStage.GameEnd:
				{
					CheckMinPlayers();
					break;
				}
			}
			OnPlayersChanged();
			RaiseStateChanged();
		}

		// Called when a player joins, leaves, or is edited.
		private void OnPlayersChanged()
		{
			RaisePlayersChanged();
			PromptBots();
			CheckRoundPlays();
		}

		/// <summary>
		/// Called whenever the stage changes.
		/// </summary>
		/// <param name="oldStage">The previous stage as of invocation.</param>
		/// <param name="currentStage">The current stage as of invocation.</param>
		private void OnStageChanged(GameStage oldStage, GameStage currentStage)
		{
			switch (currentStage)
			{
				case GameStage.RoundInProgress:
                    ClearSkipVotes();
					ClearRoundPlays();
					break;
				case GameStage.JudgingCards:
					PopulateRoundPlays();
					break;
				case GameStage.RoundEnd:
					if (oldStage == GameStage.JudgingCards)
					{
						SaveRoundPlays();
						UpdateScoreForWinningPlay();
						RoundEndTimeoutAsync();
					}
					break;
				case GameStage.GameEnd:
                    AssignTrophies();
					GameEndTimeoutAsync();
					break;
				case GameStage.GameStarting:
                    if (PlayerCount >= Settings.MinPlayers)
                    {
                        NewRound();
                        return;
                    }
					ClearRoundPlays();
					break;
			}

			PromptBots();
			RaiseStageChanged(oldStage, currentStage);
			RaiseStateChanged();
		}

        private void AssignTrophies()
        {
            foreach(var player in _players)
            {
                if (player.IsAsshole) continue;

                foreach(var trophy in _trophies)
                {
                    if (trophy.IsPlayerEligible(player))
                    {
                        player.AddTrophy(trophy);                        
                    }
                }

				foreach(var trophy in player.GetTrophies())
				{
					Console.WriteLine($"{player} earned trophy: \"{trophy.Id}\"");
				}
            }
        }

		private void SaveRoundPlays()
		{
			lock(_allPlayersSync)
			{
				foreach(var player in _players)
				{
					player.SaveCurrentPlay(RoundWinner == player);
				}
			}
		}

		private void UpdateScoreForWinningPlay()
		{
			var winningPlayer = RoundWinner;
			if (winningPlayer == null) return;
            int tierBonus = _winningPlayIndex > -1 && _winningPlayIndex < _roundPlays.Count 
                ? _roundPlays[_winningPlayIndex].Item2.Sum(c => c.Tier) 
                : 0;
			winningPlayer.AddPoints(1 + tierBonus);
            winningPlayer.AddAuxPoints(CurrentBlackCard.PickCount);
		}

		private async void RoundEndTimeoutAsync()
		{
			if (Stage != GameStage.RoundEnd) return;

			int roundNumForTimeout = _roundNum;

			await Task.Delay(Settings.RoundEndTimeout);

			if (Stage == GameStage.RoundEnd && _roundNum == roundNumForTimeout)
			{
				lock (_allPlayersSync)
				{
					// Check if any of the players have reached the winning score or max rounds are reached
					if (_players.Any(p => p.Score >= Settings.MaxPoints) || (Settings.MaxRounds > 0 && Round >= Settings.MaxRounds))
					{
						EndGame();
					}
					else
					{
						NewRound();
					}
				}
			}
		}

		private async void GameEndTimeoutAsync()
		{
			if (Stage != GameStage.GameEnd) return;

			await Task.Delay(Settings.GameEndTimeout);

			if (Stage == GameStage.GameEnd)
			{
				NewGame();
			}
		}

		private void OnPlayerJudgedCards(Player player, int winningPlayIndex)
		{
			if (!player.CanJudgeCards || winningPlayIndex < 0 || winningPlayIndex >= _roundPlays.Count) return;
			_winningPlayIndex = winningPlayIndex;
			Stage = GameStage.RoundEnd;
			RaiseRoundEnded(Round, RoundWinner);
		}

		private void OnPlayerNameChanged(Player player, string name)
		{
			OnPlayersChanged();
		}

		private void OnPlayerScoreChanged(Player player, int points)
		{
			OnPlayersChanged();
		}

		private void OnPlayerAfkChanged(Player player, bool afk)
		{
			if (afk && Judge == player)
			{
				NextJudge();
			}
			OnPlayersChanged();
			RaiseStateChanged();
		}

        #endregion

        #region Event Raisers

        private void RaiseGameEnded() => GameEnded?.Invoke(GetWinningPlayers().ToArray());

		private void RaiseRoundStarted() => RoundStarted?.Invoke();

		private void RaiseStateChanged() => GameStateChanged?.Invoke();

		private void RaisePlayersChanged() => PlayersChanged?.Invoke();

		private void RaisePlayerJoined(Player p)
		{
			PlayerJoined?.Invoke(p);
			OnPlayerCountChanged();
		}

		private void RaisePlayerLeft(Player p, string reason)
		{
			if (Judge == p)
			{
				NextJudge();
			}
			PlayerLeft?.Invoke(p, reason);
			OnPlayerCountChanged();
		}

		private void RaiseRoundEnded(int round, Player winner) => RoundEnded?.Invoke(round, winner);

		private void RaiseStageChanged(in GameStage oldStage, in GameStage currentStage) => StageChanged?.Invoke(oldStage, currentStage);

        private void RaiseBlackCardSkipped(BlackCard skippedCard, BlackCard replacementCard) => BlackCardSkipped?.Invoke(skippedCard, replacementCard);

		#endregion
	}
}
