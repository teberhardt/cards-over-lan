using CardsOverLan.Game;
using WebSocketSharp;

namespace CardsOverLan
{
    internal sealed class SpectatorConnection : ClientConnectionBase
    {
        private readonly object _createDestroyLock = new object();

        public SpectatorConnection(CardGameServer server, CardGame game) : base(server, game)
        {
        }

        protected override void OnOpen()
        {
            lock (_createDestroyLock)
            {
                base.OnOpen();

                if (!IsOpen) return;				

                if (!Server.AddSpectator(this))
                {
                    Reject(RejectCodes.ServerFull);
                    return;
                }

                SubscribeEvents();

                SendPlayerList();
                SendPackContent();
                SendGameState();
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            lock (_createDestroyLock)
            {
                base.OnClose(e);

                Server.RemoveSpectator(this);
                UnsubscribeEvents();
            }
        }

        private void SubscribeEvents()
        {
            Game.GameStateChanged += OnGameStateChanged;
            Game.PlayersChanged += OnPlayersChanged;
            Game.BlackCardSkipped += OnBlackCardSkipped;
        }

        private void UnsubscribeEvents()
        {
            Game.GameStateChanged -= OnGameStateChanged;
            Game.PlayersChanged -= OnPlayersChanged;
            Game.BlackCardSkipped -= OnBlackCardSkipped;
        }

        private void OnBlackCardSkipped(BlackCard skippedCard, BlackCard replacementCard)
        {
            SendSkipNotification(skippedCard.Id, replacementCard.Id);
        }

        private void OnGameStateChanged()
        {
            SendGameState();
        }

        private void OnPlayersChanged()
        {
            SendPlayerList();
        }
    }
}