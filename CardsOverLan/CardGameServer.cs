using CardsOverLan.Game;
using CardsOverLan.Game.Bots;
using Rant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace CardsOverLan
{
	public sealed class CardGameServer : IDisposable
	{
		private const string WebSocketListenAddress = "ws://0.0.0.0:3000";
		private const string ServerPlayDir = "/play";
		private const string ServerSpectateDir = "/spectate";
		private const int PlayerMessageLengthLimit = 300;

		private readonly WebSocketServer _ws;
		private bool _disposed = false;
		private readonly Random _rng;
		private readonly RantEngine _rant;
		private readonly HashList<SpectatorConnection> _spectators;
		private readonly HashList<ClientConnectionBase> _connections;
		private readonly Dictionary<string, int> _clientIpPool = new Dictionary<string, int>();
		private readonly Taunt[] _botTaunts;
		private readonly object _clientPoolLock = new object();
		private readonly object _spectatorLock = new object();
		private readonly object _connectionLock = new object();

		public CardGame Game { get; }

		public CardGameServer(CardGame game)
		{
			Game = game ?? throw new ArgumentNullException(nameof(game));
			Game.RoundEnded += OnGameRoundEnded;
			_rng = new Random(unchecked(Environment.TickCount * 7919));
			_rant = new RantEngine();
			_spectators = new HashList<SpectatorConnection>();
			_connections = new HashList<ClientConnectionBase>();
			_botTaunts = game.GetPacks().Select(p => p.GetTaunts()).SelectMany(t => t).ToArray();
			_ws = new WebSocketServer(WebSocketListenAddress);
			_ws.AddWebSocketService(ServerPlayDir, () => new PlayerConnection(this, Game));
			_ws.AddWebSocketService(ServerSpectateDir, () => new SpectatorConnection(this, Game));
		}

		private void OnGameRoundEnded(int round, Player roundWinner, WhiteCard[] winningPlay)
		{
			if (Game.Settings.ChatEnabled && Game.Settings.BotTauntsEnabled && _botTaunts.Length > 0)
			{
				var bots = Game.GetPlayers().Where(p => p.IsAutonomous).ToArray();
				if (bots.Length == 0) return;
				var matchingTaunts = _botTaunts.Where(t => t.IsPlayEligible(winningPlay)).ToArray();
				if (matchingTaunts.Length == 0) return;
				int maxTauntPriority = matchingTaunts.Max(t => t.Priority);
				var eligibleTaunts = matchingTaunts.Where(t => t.Priority == maxTauntPriority).ToArray();
				foreach (var bot in bots)
				{
					if (bot == roundWinner) continue;
					var activeTaunt = eligibleTaunts[_rng.Next(eligibleTaunts.Length)];
					double tauntChance = ((activeTaunt.ResponseChance / bots.Length) + activeTaunt.ResponseChance) * 0.5;
					if (_rng.NextDouble() <= tauntChance)
					{
						var responses = activeTaunt.GetResponses().ToArray();
						BotTauntAsync(bot, responses[_rng.Next(responses.Length)], roundWinner);
					}
				}
			}			
		}

		private void BotTauntAsync(Player botPlayer, LocalizedString taunt, Player winner)
		{
			var args = new RantProgramArgs();
			args["winner_name"] = winner.Name;
			SendChatMessage(botPlayer, taunt, args);
		}

		public void Start()
		{
			Console.WriteLine("Starting WebSocket services...");
			_ws.Start();
			Console.WriteLine("WebSocket services online.");
		}

		internal bool TryAddToPool(ClientConnectionBase client)
		{
			lock (_clientPoolLock)
			{
				var ip = client.GetIPAddress().ToString();
				if (ip == null) return false; // I don't even know how this would happen, but handle it anyway

				// Check if the IP is already in the pool
				if (_clientIpPool.TryGetValue(ip, out int clientCount))
				{
					if (!Game.Settings.AllowDuplicatePlayers)
					{
						return false;
					}

					// Increase client count for IP
					_clientIpPool[ip] = clientCount + 1;
				}
				else
				{
					_clientIpPool[ip] = 1;
				}

				return true;
			}
		}

		internal bool TryRemoveFromPool(ClientConnectionBase client)
		{
			lock (_clientPoolLock)
			{
				var ip = client.GetIPAddress().ToString();
				if (ip == null) return false;

				if (_clientIpPool.TryGetValue(ip, out int clientCount))
				{
					if (clientCount <= 1)
					{
						_clientIpPool.Remove(ip);
					}
					else
					{
						_clientIpPool[ip] = clientCount - 1;
					}
					return true;
				}

				return false;
			}
		}

		internal bool AddSpectator(SpectatorConnection client)
		{
			lock (_spectatorLock)
			{
				if (_spectators.Count >= Game.Settings.MaxSpectators)
				{
					return false;
				}

				return _spectators.Add(client);
			}
		}

		internal bool RemoveSpectator(SpectatorConnection client)
		{
			lock (_spectatorLock)
			{
				if (_spectators.Count == 0)
				{
					return false;
				}

				return _spectators.Remove(client);
			}
		}

		internal bool AddConnection(ClientConnectionBase connection)
		{
			lock(_connectionLock)
			{
				return _connections.Add(connection);
			}
		}

		internal bool RemoveConnection(ClientConnectionBase connection)
		{
			lock(_connectionLock)
			{
				return _connections.Remove(connection);
			}
		}

		public async void SendChatMessage(Player p, string message)
		{
			if (!Game.Settings.ChatEnabled || string.IsNullOrWhiteSpace(message)) return;
			var cleanMessageString = new string(message.Trim().Truncate(PlayerMessageLengthLimit).Where(c => !Char.IsControl(c)).ToArray());
			Console.WriteLine($"{p} says: \"{cleanMessageString}\"");
			await Task.Run(() =>
			{
				lock(_connectionLock)
				{
					foreach(var connection in _connections)
					{
						connection.SendChatMessage(p, cleanMessageString);
					}
				}
			});
		}

		public void SendChatMessage(Player p, LocalizedString message, RantProgramArgs args)
		{
			if (!Game.Settings.ChatEnabled || message == null) return;			
			int seed = _rng.Next();

			async void send(ClientConnectionBase connection, Random rng)
			{
				await Task.Delay(rng.Next(1500, 3000));
				RantProgram pgm;
				try
				{
					pgm = RantProgram.CompileString(message[connection.ClientLanguage]);
				}
				catch(RantCompilerException ex)
				{
					Console.WriteLine($"Rant compiler error: {ex}");
					return;
				}
				catch(Exception ex)
				{
					Console.WriteLine($"Rant compiler failure: {ex}");
					return;
				}
				var output = _rant.Do(pgm, seed, args: args).Main.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var msg in output)
				{
					int delay = rng.Next(750, 1100) + msg.Length * rng.Next(12, 18);
					await Task.Delay(delay);
					connection.SendChatMessage(p, msg);
					Console.WriteLine($"{p} says: \"{msg}\"");
				}
			}

			foreach (var connection in _connections.ToArray())
			{
				var rng = new Random(seed);
				send(connection, rng);
			}
		}

		public void Stop()
		{
			_ws.Stop();
		}

		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
		}
	}
}
