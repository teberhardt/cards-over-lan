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
		private readonly HashSet<IPAddress> _clientIpPool = new HashSet<IPAddress>();
        private HashSet<SpectatorConnection> _spectatorClients = new HashSet<SpectatorConnection>();
        private readonly object _spectatorLock = new object();

        public CardGameServer(CardGame game)
        {
            _game = game;
            _ws = new WebSocketServer(WebSocketListenAddress);
        }

        public void Start()
        {
            Console.WriteLine("Starting WebSocket services...");
            _ws.AddWebSocketService(ServerPlayDir, () => new ClientConnection(this, _game));
            _ws.AddWebSocketService(ServerSpectateDir, () => new SpectatorConnection(this, _game));
            _ws.Start();
			Console.WriteLine("WebSocket services online.");
        }

		internal bool TryAddToPool(ClientConnection cc)
		{
			return _clientIpPool.Add(cc.Address);
		}

		internal bool TryRemoveFromPool(ClientConnection cc)
		{
			return _clientIpPool.Remove(cc.Address);
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
