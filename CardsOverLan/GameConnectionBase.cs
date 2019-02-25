using CardsOverLan.Game;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace CardsOverLan
{
	internal abstract class GameConnectionBase : WebSocketBehavior
	{
		private IPAddress _ip;
		private readonly Dictionary<string, string> _cookies;

		public CardGameServer Server { get; }
		public CardGame Game { get; }
		public bool IsOpen => State == WebSocketSharp.WebSocketState.Open;

		public GameConnectionBase(CardGameServer server, CardGame game)
		{
			Server = server;
			Game = game;
			_cookies = new Dictionary<string, string>();
		}

		protected string GetCookie(string name)
		{
			return _cookies.TryGetValue(name, out var val) ? val : null;
		}

		public IPAddress GetIPAddress() => _ip;

		private void LoadCookies()
		{
			foreach (WebSocketSharp.Net.Cookie cookie in Context.CookieCollection)
			{
				_cookies[cookie.Name] = HttpUtility.UrlDecode(cookie.Value);
			}
		}

		protected override void OnOpen()
		{
			base.OnOpen();
			LoadCookies();
			_ip = Context.UserEndPoint.Address;
		}

		protected void SendGameState()
		{
			SendMessageObject(new
			{
				msg = "s_gamestate",
				stage = Game.Stage,
				round = Game.Round,
				black_card = Game.CurrentBlackCard?.ID,
				pending_players = Game.GetPendingPlayers().Select(p => p.Id),
				judge = Game.Judge?.Id ?? -1,
				plays = Game.GetRoundPlays().Select(p => p.Item2.Select(c => c.ID)),
				winning_play = Game.WinningPlayIndex,
				winning_player = Game.RoundWinner?.Id ?? -1,
				game_results = Game.Stage == GameStage.GameEnd
					? new
					{
						winners = Game.GetWinningPlayers().Select(p => p.Id),
						trophy_winners = Game.GetPlayers().Select(p => new
						{
							id = p.Id,
							trophies = p.GetTrophies()
						})
					}
					: null
			});
		}

		protected void SendPlayerList()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_players",
				players = Game.GetPlayers().Select(p => new
				{
					name = HttpUtility.HtmlEncode(p.Name),
					id = p.Id,
					score = p.Score,
					upgrade_points = p.Coins
				})
			});
		}

		protected void SendPackContent()
		{
			var response = new
			{
				msg = "s_allcards",
				packs = Game.GetPacks()
			};
			SendMessageObject(response);
		}

		protected void SendRejection(string rejectReason, string rejectDesc = "")
		{
			SendMessageObject(new
			{
				msg = "s_rejectclient",
				reason = rejectReason,
				desc = rejectDesc
			});
		}

		protected void Reject(string rejectReason, string rejectDesc = "")
		{
			SendRejection(rejectReason, rejectDesc);
			Context.WebSocket.Close(CloseStatusCode.Normal, rejectReason);
		}

		protected void SendMessageObject(object o)
		{
			Send(JsonConvert.SerializeObject(o, Formatting.None));
		}
	}
}
