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

			Console.WriteLine("\n========= GAME STARTING =========\n");
			Console.WriteLine($"Player limit: [{Settings.MinPlayers}, {Settings.MaxPlayers}]");
			Console.WriteLine($"Hand size: {Settings.HandSize}");
			Console.WriteLine($"Perma-Czar: {Settings.PermanentCzar}");
			Console.WriteLine($"Bot Czars: {Settings.AllowBotCzars}");
			Console.WriteLine($"Winner Is Czar: {Settings.WinnerCzar}");
			Console.WriteLine($"Points to win: {Settings.MaxPoints}");
			Console.WriteLine($"Max Rounds: {Settings.MaxRounds}");
			Console.WriteLine($"Upgrades enabled: {Settings.UpgradesEnabled}");
			Console.WriteLine($"Allow duplicate players: {Settings.AllowDuplicatePlayers}");
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
			Console.WriteLine($"Black card skipped: {skippedCard.ID} -> {replacementCard.ID}");
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
				upgrades_enabled = Game.Settings.UpgradesEnabled,
				perma_czar = Game.Settings.PermanentCzar,
				bot_czars = Game.Settings.AllowBotCzars,
				bot_count = Game.Settings.BotCount,
				winner_czar = Game.Settings.WinnerCzar,
				max_points = Game.Settings.MaxPoints,
				max_rounds = Game.Settings.MaxRounds,
				blank_cards = Game.Settings.BlankCards,
				discards = Game.Settings.Discards,
				allow_skips = Game.Settings.AllowBlackCardSkips,
				pack_info = _packs.Select(p => new { id = p.Id, name = p.Name })
			};
		}

		private void OnGameEnded(Player[] winners)
		{
			Console.WriteLine($"Game ended. Winners: {winners.Select(w => w.ToString()).Aggregate((c, n) => $"{c}, {n}")}");
		}

		private void OnGameRoundEnded(int round, Player roundWinner)
		{
			Console.WriteLine($"Round {round} ended: {roundWinner?.ToString() ?? "Nobody"} wins!");
		}

		private void OnGameStageChanged(in GameStage oldStage, in GameStage currentStage)
		{
			Console.WriteLine($"Stage changed: {oldStage} -> {currentStage}");
		}

		private void OnGameRoundStarted()
		{
			Console.WriteLine($"ROUND {Game.Round}:");
			Console.WriteLine($"Current black card: {Game.CurrentBlackCard} (draw {Game.CurrentBlackCard.DrawCount}, pick {Game.CurrentBlackCard.PickCount})");
			Console.WriteLine($"Judge is {Game.Judge}");
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
