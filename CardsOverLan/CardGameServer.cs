using CardsOverLan.Game;
using System;
using System.Collections.Generic;
using System.Net;
using WebSocketSharp.Server;

namespace CardsOverLan
{
    public sealed class CardGameServer : IDisposable
    {
        private readonly WebSocketServer _ws;
        private readonly CardGame _game;
		private bool _disposed = false;
		private readonly HashSet<IPAddress> _clientIpPool = new HashSet<IPAddress>();

        public CardGameServer(CardGame game)
        {
            _game = game;
            _ws = new WebSocketServer("ws://0.0.0.0:3000");
        }

        public void Start()
        {
            _ws.AddWebSocketService("/game", () => new ClientConnection(this, _game));
            _ws.Start();
			Console.WriteLine("WebSocket server active");
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
