using CardsOverLan.Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan
{
	internal sealed class GameManager
	{
		private const string PacksDirectory = "packs";
		private const string SettingsFilePath = "settings.json";

		public static GameManager Instance { get; }

		public GameSettings Settings { get; }

		public CardGame Game { get; }

		private readonly List<Pack> _packs;

		static GameManager()
		{
			Instance = new GameManager();
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public static void Load()
		{
		}

		private GameManager()
		{
			_packs = new List<Pack>();

			// Load the settings
			Settings = GameSettings.FromFile(SettingsFilePath);

			// Load all the decks
			foreach (var deckPath in Directory.EnumerateFiles(PacksDirectory, "*.json", SearchOption.AllDirectories))
			{
				try
				{
					var pack = JsonConvert.DeserializeObject<Pack>(File.ReadAllText(deckPath));
					_packs.Add(pack);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to load deck '{deckPath}': {ex}");
				}
			}

			Game = new CardGame(_packs, Settings);

			Console.WriteLine("\n=========== GAME INFO ===========\n");
			Console.WriteLine($"Player limit: [{Settings.MinPlayers}, {Settings.MaxPlayers}]");
			Console.WriteLine($"Hand size: {Settings.HandSize}");
			Console.WriteLine($"Perma-Czar: {Settings.PermanentCzar}");
			Console.WriteLine($"Bot Czars: {Settings.AllowBotCzars}");
			Console.WriteLine($"Winner Is Czar: {Settings.WinnerCzar}");
			Console.WriteLine($"Points to win: {Settings.MaxPoints}");
			Console.WriteLine($"Max Rounds: {Settings.MaxRounds}");
			Console.WriteLine($"Upgrades enabled: {Settings.UpgradesEnabled}");
			Console.WriteLine($"Allow duplicate players: {Settings.AllowDuplicatePlayers}");
			Console.WriteLine($"Player Preserves: {Settings.PlayerPreserveEnabled}");
			if (Settings.PlayerPreserveEnabled)
			{
				Console.WriteLine($"Player Preserve Time: {Settings.PlayerPreserveTimeSeconds}s");
			}
			Console.WriteLine($"Cards: {Game.BlackCardCount + Game.WhiteCardCount} ({Game.WhiteCardCount}x white, {Game.BlackCardCount}x black)");
			Console.WriteLine();
			Console.WriteLine($"Packs:\n{Game.GetPacks().Select(d => $"        [{d}]").Aggregate((c, n) => $"{c}\n{n}")}");
			Console.WriteLine("\n=================================\n");

			Game.GameStateChanged += OnGameStateChanged;
			Game.RoundStarted += OnGameRoundStarted;
			Game.StageChanged += OnGameStageChanged;
			Game.RoundEnded += OnGameRoundEnded;
			Game.GameEnded += OnGameEnded;
			Game.BlackCardSkipped += OnBlackCardSkipped;

			UpdateTitle();
		}

		private void OnBlackCardSkipped(BlackCard skippedCard, BlackCard replacementCard)
		{
			Console.WriteLine($"SKIPPED BLACK CARD: {skippedCard.ID} -> {replacementCard.ID}");
		}

		public object GetGameInfoObject()
		{
			return new
			{
				server_name = Settings.ServerName,
				min_players = Settings.MinPlayers,
				current_player_count = Game.PlayerCount,
				max_players = Settings.MaxPlayers,
				hand_size = Settings.HandSize,
				white_card_count = Game.WhiteCardCount,
				black_card_count = Game.BlackCardCount,
				upgrades_enabled = Settings.UpgradesEnabled,
				perma_czar = Settings.PermanentCzar,
				bot_czars = Settings.AllowBotCzars,
				bot_count = Settings.BotCount,
				winner_czar = Settings.WinnerCzar,
				max_points = Settings.MaxPoints,
				max_rounds = Settings.MaxRounds,
				blank_cards = Settings.BlankCards,
				discards = Settings.Discards,
				allow_skips = Settings.AllowBlackCardSkips,
				chat_enabled = Settings.ChatEnabled,
				pack_info = _packs.Select(p => new { id = p.Id, name = p.Name }),
				game_port = Settings.ClientWebSocketPort
			};
		}

		private void OnGameEnded(Player[] winners)
		{
			Console.WriteLine($"GAME OVER: Winners: {winners.Select(w => w.ToString()).Aggregate((c, n) => $"{c}, {n}")}");
		}

		private void OnGameRoundEnded(int round, BlackCard blackCard, Player roundJudge, Player roundWinner, bool ego, WhiteCard[] winningPlay)
		{
			Console.WriteLine($"Round {round} ended: {roundWinner?.ToString() ?? "Nobody"} wins!");
		}

		private void OnGameStageChanged(in GameStage oldStage, in GameStage currentStage)
		{
			Console.WriteLine($"STAGE CHANGE: {oldStage} -> {currentStage}");
		}

		private void OnGameRoundStarted()
		{
			Console.WriteLine($"ROUND {Game.Round}:");
			Console.WriteLine($"BLACK CARD: {Game.CurrentBlackCard} (draw {Game.CurrentBlackCard.DrawCount}, pick {Game.CurrentBlackCard.PickCount})");
			Console.WriteLine($"CARD CZAR: {Game.Judge}");
		}

		private void OnGameStateChanged()
		{
			UpdateTitle();
		}

		private void UpdateTitle()
		{
			Console.Title = $"Cards Over LAN Server ({Game.PlayerCount})";
		}
	}
}
