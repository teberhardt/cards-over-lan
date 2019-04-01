using CardsOverLan.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace CardsOverLan
{
	internal sealed class SpectatorConnection : ClientConnectionBase
	{
		private bool _isRejectedDuplicate;
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
				// s_clientinfo?
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
			SendSkipNotification(skippedCard.ID, replacementCard.ID);
		}

		private void OnGameStateChanged()
		{
			SendGameState();
		}

		private void OnPlayersChanged()
		{
			SendPlayerList();
		}

		protected override void OnError(ErrorEventArgs e)
		{
			base.OnError(e);
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			base.OnMessage(e);
		}
	}
}
