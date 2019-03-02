using CardsOverLan.Game;
using System;
using System.Collections.Generic;
using System.Net;
using WebSocketSharp.Server;

namespace CardsOverLan
{
    public sealed class CardGameServer : IDisposable
    {
        private const string WebSocketListenAddress = "ws://0.0.0.0:3000";
        private const string ServerPlayDir = "/play";
        private const string ServerSpectateDir = "/spectate";

        private readonly WebSocketServer _ws;
        private readonly CardGame _game;
		private bool _disposed = false;
		private readonly HashList<SpectatorConnection> _spectators;
		private readonly Dictionary<string, int> _clientIpPool = new Dictionary<string, int>();
        private readonly object _clientPoolLock = new object();
		private readonly object _spectatorLock = new object();

        public CardGameServer(CardGame game)
        {
            _game = game;
			_spectators = new HashList<SpectatorConnection>();
            _ws = new WebSocketServer(WebSocketListenAddress);
			_ws.AddWebSocketService(ServerPlayDir, () => new PlayerConnection(this, _game));
			_ws.AddWebSocketService(ServerSpectateDir, () => new SpectatorConnection(this, _game));
		}

        public void Start()
        {
            Console.WriteLine("Starting WebSocket services...");            
            _ws.Start();
			Console.WriteLine("WebSocket services online.");
        }

		internal bool TryAddToPool(ClientConnectionBase client)
		{
			lock(_clientPoolLock)
			{
				var ip = client.GetIPAddress().ToString();
				if (ip == null) return false; // I don't even know how this would happen, but handle it anyway

				// Check if the IP is already in the pool
				if (_clientIpPool.TryGetValue(ip, out int clientCount))
				{
					if (!_game.Settings.AllowDuplicatePlayers)
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
			lock(_clientPoolLock)
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
			lock(_spectatorLock)
			{
				if (_spectators.Count >= _game.Settings.MaxSpectators)
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
