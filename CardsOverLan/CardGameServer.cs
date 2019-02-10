using CardsOverLan.Game;
using System;
using WebSocketSharp.Server;

namespace CardsOverLan
{
    public sealed class CardGameServer : IDisposable
    {
        private readonly WebSocketServer _ws;
        private readonly CardGame _game;
		private bool _disposed = false;

        public CardGameServer(CardGame game)
        {
            _game = game;
            _ws = new WebSocketServer("ws://0.0.0.0:3000");
        }

        public void Start()
        {
            _ws.AddWebSocketService("/game", () => new ClientConnection(_game));
            _ws.Start();
			Console.WriteLine("WebSocket server active");
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
