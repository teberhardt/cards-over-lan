using CardsOverLan.Game;
using CardsOverLan.Game.Bots;
using Rant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace CardsOverLan
{
	public sealed class CardGameServer : IDisposable
	{
		private const string ServerPlayDir = "/play";
		private const string ServerSpectateDir = "/spectate";
		private const int PlayerMessageLengthLimit = 300;

		private readonly WebSocketServer _ws;
		private bool _disposed;
		private readonly Random _rng;
		private readonly RantEngine _rant;
		private readonly HashList<SpectatorConnection> _spectators;
		private readonly HashList<ClientConnectionBase> _connections;
		private readonly Tally<string, ClientConnectionBase> _addressTally;
		private readonly Taunt[] _botTaunts;
		private readonly object _spectatorLock = new object();
		private readonly object _connectionLock = new object();

		private CardGame Game { get; }

		public CardGameServer(CardGame game)
		{
			Game = game ?? throw new ArgumentNullException(nameof(game));
			Game.RoundEnded += OnGameRoundEnded;
			Game.GameEnded += OnGameEnded;
			_rng = new Random(unchecked(Environment.TickCount * 7919));
			_rant = new RantEngine();
			_spectators = new HashList<SpectatorConnection>();
			_connections = new HashList<ClientConnectionBase>();
			_addressTally = new Tally<string, ClientConnectionBase>();
			_botTaunts = game.GetPacks().Select(p => p.GetTaunts()).SelectMany(t => t).ToArray();
			_ws = new WebSocketServer(game.Settings.WebSocketUrl);
			_ws.AddWebSocketService(ServerPlayDir, () => new PlayerConnection(this, Game));
			_ws.AddWebSocketService(ServerSpectateDir, () => new SpectatorConnection(this, Game));
		}

		private void OnGameEnded(Player[] winners)
		{
			TriggerBotTaunts(null,
				bot => EnumerateEventTaunts(bot, "game_end", b => true));
		}

		private void OnGameRoundEnded(int round, BlackCard blackCard, Player roundJudge, Player roundWinner, bool ego, WhiteCard[] winningPlay)
		{
			var czar = Game.Judge;			

			var args = new
			{
				winner = roundWinner.Name
			};

			TriggerBotTaunts(args,
				bot => EnumerateCardTaunts(bot, czar, roundWinner, winningPlay),
				bot => EnumerateEventTaunts(bot, "lost_round", b => b != roundWinner && b != czar));
		}

		private IEnumerable<Taunt> EnumerateEventTaunts(Player bot, string eventName, Func<Player, bool> botPredicate)
		{
			if (!botPredicate(bot)) yield break;
			foreach(var taunt in _botTaunts.Where(t => t.TriggerEvent == eventName))
			{
				yield return taunt;
			}
		}

		private IEnumerable<Taunt> EnumerateCardTaunts(Player bot, Player czar, Player roundWinner, IEnumerable<WhiteCard> winningPlay)
		{
			if (bot == roundWinner || bot == czar) yield break;
			foreach(var taunt in _botTaunts.Where(t => t.IsPlayEligible(winningPlay)))
			{
				yield return taunt;
			}
		}

		private void TriggerBotTaunts(object args, params Func<Player, IEnumerable<Taunt>>[] tauntSelectors)
		{
			if (!Game.Settings.ChatEnabled || !Game.Settings.BotTauntsEnabled || _botTaunts.Length <= 0) return;
			var bots = Game.GetPlayers().Where(p => p.IsAutonomous).ToArray();
			if (bots.Length == 0) return;

			var matchingTaunts = new HashList<Taunt>();

			foreach (var bot in bots)
			{
				matchingTaunts.Clear();					

				matchingTaunts.AddRange(tauntSelectors.SelectMany(selector => selector(bot)));

				BotSelectTaunt(bot, bots.Length, matchingTaunts, args);
			}
		}

		private void BotSelectTaunt(Player botPlayer, int botCount, ICollection<Taunt> matchingTaunts, object args)
		{
			// Skip this bot if there are no matches
			if (matchingTaunts.Count == 0) return;

			// Get highest priority in taunt matches
			var maxTauntPriority = matchingTaunts.Max(t => t.Priority);

			// Get taunts matching highest priority
			var eligibleCardTaunts = matchingTaunts.Where(t => t.Priority == maxTauntPriority).ToArray();

			// Select a taunt
			var activeTaunt = eligibleCardTaunts[_rng.Next(eligibleCardTaunts.Length)];

			// Calculate taunt probability
			double tauntChance = ((activeTaunt.ResponseChance / botCount) + activeTaunt.ResponseChance) * 0.5;

			// Check against probability and send message
			if (!(_rng.NextDouble() <= tauntChance)) return;
			var responses = activeTaunt.GetResponses().ToArray();
			SendBotChatMessage(botPlayer, responses[_rng.Next(responses.Length)], RantProgramArgs.CreateFrom(args));
		}

		public void Start()
		{
			_ws.Start();
		}

		internal bool IsIpConnected(string ip)
		{
			return _addressTally.HasKey(ip);
		}

		internal bool AddSpectator(SpectatorConnection client)
		{
			lock (_spectatorLock)
			{
				return _spectators.Count < Game.Settings.MaxSpectators && _spectators.Add(client);
			}
		}

		internal void RemoveSpectator(SpectatorConnection client)
		{
			lock (_spectatorLock)
			{
				if (_spectators.Count == 0)
				{
					return;
				}

				_spectators.Remove(client);
			}
		}

		internal void AddConnection(ClientConnectionBase connection)
		{
			lock (_connectionLock)
			{
				var unused = _addressTally.AddTally(connection.GetIpAddress(), connection) && _connections.Add(connection);
			}
		}

		internal void RemoveConnection(ClientConnectionBase connection)
		{
			lock (_connectionLock)
			{
				var unused = _addressTally.RemoveTally(connection.GetIpAddress(), connection) && _connections.Remove(connection);
			}
		}

		public async void SendChatMessage(Player p, string message)
		{
			if (!Game.Settings.ChatEnabled || string.IsNullOrWhiteSpace(message)) return;
			var cleanMessageString = new string(message.Trim().Truncate(PlayerMessageLengthLimit).Where(c => !char.IsControl(c)).ToArray());
			Console.WriteLine($"{p} says: \"{cleanMessageString}\"");
			await Task.Run(() =>
			{
				lock (_connectionLock)
				{
					foreach (var connection in _connections)
					{
						connection.SendChatMessage(p, cleanMessageString);
					}
				}
			});
		}

		private void SendBotChatMessage(Player p, LocalizedString message, RantProgramArgs args)
		{
			if (!Game.Settings.ChatEnabled || message == null) return;
			var seed = _rng.Next();
			var cfg = Game.Settings.BotConfig;

			async void Send(ClientConnectionBase connection, Random rng)
			{
				await Task.Delay(rng.Next(1500, 3000));
				try
				{
					var pgm = RantProgram.CompileString(message[connection.ClientLanguage]);
					var output = _rant.Do(pgm, seed, 0, -1, args).Main.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var msg in output)
					{
						var delay = rng.Next(cfg.MinTypingDelay, cfg.MaxTypingDelay + 1) 
							+ msg.Length * rng.Next(cfg.MinTypingInterval, cfg.MaxTypingInterval + 1);
						await Task.Delay(delay);
						connection.SendChatMessage(p, msg);
						Console.WriteLine($"{p} says: \"{msg}\"");
					}
				}
				catch (RantCompilerException ex)
				{
					Console.WriteLine($"Rant compiler error: {ex}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Rant failure: {ex}");
				}
			}

			foreach (var connection in _connections.ToArray())
			{
				var rng = new Random(seed);
				Send(connection, rng);
			}
		}

		private void Stop()
		{
			_ws.Stop();
		}

		public void Dispose()
		{
			if (_disposed) return;
			if (_ws.IsListening)
			{
				Stop();
			}
			_disposed = true;
		}
	}
}
