using CardsOverLan.Game;
using CardsOverLan.Game.ContractResolvers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace CardsOverLan
{
	internal abstract class ClientConnectionBase : WebSocketBehavior
	{
		private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = ClientFacingContractResolver.Instance
		};

		private const string DefaultLanguage = "en";

		private string _ip = string.Empty;
		private readonly Dictionary<string, string> _cookies;

		public bool IsRejected { get; private set; }

		public CardGameServer Server { get; }
		public CardGame Game { get; }
		public string ClientLanguage { get; private set; } = DefaultLanguage;
		public bool IsOpen => State == WebSocketState.Open;

		public ClientConnectionBase(CardGameServer server, CardGame game)
		{
			Server = server;
			Game = game;
			_cookies = new Dictionary<string, string>();
		}

		protected string GetCookie(string name, string fallback = null)
		{
			return _cookies.TryGetValue(name, out var val) ? val : fallback;
		}

		public string GetIpAddress() => _ip;

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

			// Get the client's original IP
			var xForwardedFor = Context.Headers["X-Forwarded-For"] ?? string.Empty;
			var forwardedAddresses = xForwardedFor.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
			_ip = forwardedAddresses.Length > 0 ? forwardedAddresses[0].ToLowerInvariant() : Context.UserEndPoint.Address.ToString();

			// Kick duplicates
			if (!Game.Settings.AllowDuplicatePlayers && Server.IsIpConnected(_ip))
			{
				Reject("reject_duplicate");
				return;
			}

			// Verify password
			if (string.IsNullOrEmpty(Game.Settings.ServerPassword) || GetCookie("game_password") == Game.Settings.ServerPassword)
			{
				ClientLanguage = GetCookie("client_lang", "en");
				Server.AddConnection(this);				
			}
			else
			{
				Reject("reject_bad_password");
			}
		}

		protected override void OnClose(CloseEventArgs e)
		{
			base.OnClose(e);
			Server.RemoveConnection(this);
		}

		protected void SendGameState()
		{
			SendMessageObject(new
			{
				msg = "s_gamestate",
				stage = Game.Stage,
				ready_up = Game.ReadyUpActive,
				round = Game.Round,
				black_card = Game.CurrentBlackCard?.Id,
				pending_players = Game.GetPendingPlayers().Select(p => p.Id),
				judge = Game.Judge?.Id ?? -1,
				plays = Game.GetRoundPlays().Select(p => p.Item2.Select(c => c.Id)),
				winning_play = Game.WinningPlayIndex,
				winning_player = Game.RoundWinner?.Id ?? -1,
				judge_voted_self = Game.JudgeVotedSelf,
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

		internal void SendChatMessage(Player p, string message)
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_chat_msg",
				author = p.Name,
				body = message
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
					upgrade_points = p.Coins,
					voted_skip = p.VotedForBlackCardSkip,
					idle = p.IsAfk,
					is_bot = p.IsAutonomous,
					ready_up = p.ReadyUp
				})
			});
		}

		protected void SendPackContent()
		{
			if (!IsOpen) return;
			var response = new
			{
				msg = "s_allcards",
				packs = Game.GetPacks()
			};
			SendMessageObject(response);
		}

		protected void SendSkipNotification(string skippedCardId, string replacementCardId)
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_notify_skipped",
				skipped_id = skippedCardId,
				replacement_id = replacementCardId
			});
		}

		private void SendRejection(string rejectReason, string rejectDesc = "")
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
			IsRejected = true;
			SendRejection(rejectReason, rejectDesc);
			Context.WebSocket.Close(CloseStatusCode.Normal, rejectReason);
		}

		protected virtual void SendMessageObject(object o)
		{
			if (!IsOpen) return;
			Send(JsonConvert.SerializeObject(o, Formatting.None, SerializerSettings));
		}
	}
}
