using CardsOverLan.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace CardsOverLan
{
    internal sealed class SpectatorConnection : WebSocketBehavior
    {
        public SpectatorConnection(CardGameServer server, CardGame game)
        {
            Server = server;
            Game = game;
        }

        public CardGameServer Server { get; }
        public CardGame Game { get; }
    }
}
