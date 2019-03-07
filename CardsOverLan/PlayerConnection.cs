using CardsOverLan.Game;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace CardsOverLan
{
	internal sealed class PlayerConnection : ClientConnectionBase
	{
		private static readonly HashSet<char> AllowedCustomCardChars = new HashSet<char>(new[] { ' ', '$', '\"', '\'', '(', ')', '%', '!', '?', '&', ':', '/', ',', '.', '@' });

		private readonly object _createDestroySync = new object();
		private bool _removalNotified;
		private Player _player;
		private Thread _afkCheckThread;
		private int _inactiveTime;
		private readonly object _afkLock = new object();
		private bool _isRejectedDuplicate;
		public Player Player => _player;

		public PlayerConnection(CardGameServer server, CardGame game) : base(server, game)
		{
			_afkCheckThread = new Thread(AfkCheckThread);
		}

		private void UpdateActivityTime(int time)
		{
			lock (_afkLock)
			{
				if (Player != null && time > 0)
				{
					Player.IsAfk = false;
				}
				_inactiveTime = time;
			}
		}

		private void AfkCheckThread()
		{
			while (IsOpen)
			{
				lock (_afkLock)
				{
					bool afk = _inactiveTime == 0;
					if (Player.IsAfk != afk)
					{
						bool isAfkEligible = (!Player.IsSelectionValid && Game.Stage == GameStage.RoundInProgress)
							|| (Game.Judge == Player && (Game.Stage == GameStage.RoundInProgress || Game.Stage == GameStage.JudgingCards));
						if (afk && isAfkEligible)
						{
							Player.IsAfk = true;
							Console.WriteLine($"{Player} is AFK (inactive for {Game.Settings.AfkTimeSeconds}s)");
						}
						else
						{
							Player.IsAfk = false;
						}
					}
				}
				Thread.Sleep(1000);
				_inactiveTime = _inactiveTime > 0 ? _inactiveTime - 1 : 0;
			}
		}

		private void CreatePlayer()
		{
			_player = Game.CreatePlayer(GetCookie("name"));

			RegisterEvents();

			SendClientInfoToPlayer();
			SendPlayerList();
			SendPackContent();
			SendHandToPlayer();
			SendGameState();
			SendAuxDataToPlayer();
		}


		private void RegisterEvents()
		{
			Player.CardsChanged += OnPlayerCardsChanged;
			Player.SelectionChanged += OnPlayerSelectionChanged;
			Player.NameChanged += OnPlayerNameChanged;
			Player.AuxDataChanged += OnPlayerAuxDataChanged;
			Game.GameStateChanged += OnGameStateChanged;
			Game.PlayersChanged += OnGamePlayersChanged;
			Game.StageChanged += OnGameStageChanged;
			Game.BlackCardSkipped += OnBlackCardSkipped;
		}

		private void UnregisterEvents()
		{
			if (Player != null)
			{
				Player.CardsChanged -= OnPlayerCardsChanged;
				Player.SelectionChanged -= OnPlayerSelectionChanged;
				Player.NameChanged -= OnPlayerNameChanged;
				Player.AuxDataChanged -= OnPlayerAuxDataChanged;
			}
			Game.GameStateChanged -= OnGameStateChanged;
			Game.PlayersChanged -= OnGamePlayersChanged;
			Game.StageChanged -= OnGameStageChanged;
			Game.BlackCardSkipped -= OnBlackCardSkipped;
		}

		private void OnBlackCardSkipped(BlackCard skippedCard, BlackCard replacementCard)
		{
			SendSkipNotification(skippedCard.ID, replacementCard.ID);
		}

		private void OnGamePlayersChanged()
		{
			SendPlayerList();
		}

		private void OnPlayerAuxDataChanged(Player player)
		{
			SendAuxDataToPlayer();
		}

		private void OnPlayerNameChanged(Player player, string name)
		{
			SendClientInfoToPlayer();
		}

		private void OnPlayerSelectionChanged(Player player, WhiteCard[] selection)
		{
			SendSelectionToPlayer();
		}

		private void OnPlayerCardsChanged(Player player, WhiteCard[] cards)
		{
			SendHandToPlayer();
		}

		private void OnGameStateChanged()
		{
			SendGameState();
		}

		private void OnGameStageChanged(in GameStage oldStage, in GameStage currentStage)
		{
			// Players who were waiting for a game to start aren't exactly AFK
			if (oldStage == GameStage.GameStarting && currentStage == GameStage.RoundInProgress)
			{
				UpdateActivityTime(Game.Settings.AfkTimeSeconds);
			}
			else if (Player.IsAfk && currentStage == GameStage.RoundInProgress)
			{
				UpdateActivityTime(Game.Settings.AfkRecoveryTimeSeconds);
			}
		}

		private void SendHandToPlayer()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_hand",
				blanks = _player.RemainingBlankCards,
				hand = _player.GetCurrentHand().Select(c => c.ID),
				discards = _player.Discards
			});
		}

		private void SendClientInfoToPlayer()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_clientinfo",
				player_id = Player.Id,
				player_name = Player.Name
			});
		}

		private void SendSelectionToPlayer()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_cardsplayed",
				selection = _player.GetSelectedCards().Select(c => c.ID)
			});
		}

		private void SendAuxDataToPlayer()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_auxclientdata",
				aux_points = _player.Coins
			});
		}

		protected override void OnOpen()
		{
			lock (_createDestroySync)
			{
				base.OnOpen();

				// Make sure player can actually join
				if (Game.PlayerCount >= Game.Settings.MaxPlayers)
				{
					Reject(RejectCodes.ServerFull);
					return;
				}
				else if (!Server.TryAddToPool(this))
				{
					_isRejectedDuplicate = true;
					Reject(RejectCodes.Duplicate);
					return;
				}

				CreatePlayer();
				UpdateActivityTime(Game.Settings.AfkTimeSeconds);
				_afkCheckThread.Start();
				Console.WriteLine($"{Player} ({Context.UserEndPoint.Address}) connected");
			}
		}

		protected override void OnClose(CloseEventArgs e)
		{
			lock (_createDestroySync)
			{
				base.OnClose(e);

				if (!_isRejectedDuplicate)
				{
					Server.TryRemoveFromPool(this);
				}

				UnregisterEvents();

				string closeReason = e.Reason;
				if (string.IsNullOrWhiteSpace(closeReason))
				{
					switch (e.Code)
					{
						case 1000:
						case 1001:
							closeReason = "User disconnected";
							break;
						case 1002:
							closeReason = "Protocol error";
							break;
						case 1003:
							closeReason = "Received unsupported data type";
							break;
						case 1005:
							closeReason = "No status code given";
							break;
						case 1006:
							closeReason = "Connection closed abnormally";
							break;
						case 1007:
							closeReason = "Invalid message type";
							break;
						case 1008:
							closeReason = "Policy violation";
							break;
						case 1009:
							closeReason = "Message too large";
							break;
						case 1010:
							closeReason = "Failed handshake";
							break;
						case 1011:
							closeReason = "Unable to fulfill client request";
							break;
						case 1015:
							closeReason = "Failed TLS handshake";
							break;
						default:
							closeReason = "Unknown";
							break;
					}
				}

				if (Game.RemovePlayer(Player, closeReason))
				{
					Console.WriteLine($"{Player} disconnected: {closeReason} (code {e.Code})");
				}
			}
		}

		// Used when player is manually disconnected by server
		internal void NotifyRemoval(string reason)
		{
			if (!IsOpen) return;
			_removalNotified = true;
			Console.WriteLine($"Player {Player} disconnected by server ({reason})");
			Context.WebSocket.Close();
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			base.OnMessage(e);
			var json = JToken.Parse(e.Data) as JObject;
			if (json == null) return;

			var msg = json["msg"]?.Value<string>();
			if (msg == null) return;

			switch (msg)
			{
				case "c_updateinfo":
				{
					var userInfoObject = json["userinfo"] as JObject;
					if (userInfoObject == null) break;
					foreach (var key in userInfoObject)
					{
						switch (key.Key.ToLowerInvariant())
						{
							case "name":
							{
								var strName = key.Value.Value<string>();
								if (strName != Player.Name)
								{
									var oldName = Player.Name;
									var name = Game.CreatePlayerName(strName, Player);
									Player.Name = name;
									Console.WriteLine($"{oldName} changed their name to {name}");
								}
								break;
							}
						}
					}
					UpdateActivityTime(Game.Settings.AfkTimeSeconds);
					break;
				}
				case "c_playcards":
				{
					var cardArray = (json["cards"] as JArray)?
						.Select(v => GetCardFromId(v.Value<string>()))?
						.OfType<WhiteCard>()?.ToArray();

					if (cardArray == null) break;
					Player.PlayCards(cardArray);
					UpdateActivityTime(Game.Settings.AfkTimeSeconds);
					break;
				}
				case "c_judgecards":
				{
					var winningPlayIndex = json["play_index"]?.Value<int>() ?? -1;
					if (winningPlayIndex < 0) break;
					Player.JudgeCards(winningPlayIndex);
					UpdateActivityTime(Game.Settings.AfkTimeSeconds);
					break;
				}
				case "c_upgradecard":
				{
					var requestedUpgradeCard = Game.GetCardById(json["card_id"]?.Value<string>()) as WhiteCard;
					Player.UpgradeCard(requestedUpgradeCard);
					break;
				}
				case "c_discardcard":
				{
					var requestedDiscardCard = Game.GetCardById(json["card_id"]?.Value<string>()) as WhiteCard;
					Player.DiscardCard(requestedDiscardCard);
					break;
				}
				case "c_vote_skip":
				{
					bool voted = json["voted"]?.Value<bool>() ?? false;
					Player.SetSkipVoteState(voted);
					break;
				}
			}
		}

		private Card GetCardFromId(string id)
		{
			const string customFlag = "custom:";
			const string customContentXss = "Trying to hack a card game, of all things.";
			const int maxCustomTextLength = 72;

			var idTrimmed = id?.Trim();
			if (String.IsNullOrWhiteSpace(idTrimmed)) return null;

			// Is it a custom card?
			if (id.StartsWith(customFlag))
			{
				var customContent = idTrimmed.Substring(customFlag.Length).Trim();
				if (customContent.Length == 0) return null;

				// Make sure there's nothing sketchy in the card text.
				bool xss = false;
				var sanitizedContent = StringUtilities.SanitizeClientString(
					customContent,
					maxCustomTextLength,
					ref xss,
					c => !Char.IsControl(c) && (Char.IsLetterOrDigit(c) || AllowedCustomCardChars.Contains(c)));

				// :)
				if (xss)
				{
					Player.AddPoints(-69);
					sanitizedContent = customContentXss;
					Player.IsAsshole = true;
				}

				return Card.CreateCustom(sanitizedContent);
			}

			return Game.GetCardById(id);
		}
	}
}
