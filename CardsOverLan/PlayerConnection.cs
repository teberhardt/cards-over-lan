using CardsOverLan.Game;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using WebSocketSharp;

namespace CardsOverLan
{
	internal sealed class PlayerConnection : ClientConnectionBase
	{
		private readonly object _createDestroySync = new object();
		private readonly Thread _idleCheckThread;
		private int _inactiveTime, _afkTime;
		private bool _afkRecovery;
		private readonly object _afkLock = new object();
		private Player Player { get; set; }

		public PlayerConnection(CardGameServer server, CardGame game) : base(server, game)
		{
			_idleCheckThread = new Thread(IdleCheckThread);
		}

		private void ResetIdleTime(bool afkOnly)
		{
			lock (_afkLock)
			{
				_afkTime = 0;
				if (!afkOnly) _inactiveTime = 0;
			}
		}

		private void SetAfkState(bool idle)
		{
			if (Player != null)
			{
				Player.IsAfk = idle;
			}
		}

		private void IdleCheckThread()
		{
			while (IsOpen)
			{
				lock (_afkLock)
				{
					var isAfkEligible = 
						Game.Judge == Player && Game.Stage == GameStage.JudgingCards
						|| Game.Judge != Player && !Player.IsSelectionValid && Game.Stage == GameStage.RoundInProgress;
					var shouldKick = _inactiveTime >= Game.Settings.IdleKickTimeSeconds;
					var afk = _afkRecovery ? _afkTime >= Game.Settings.AfkRecoveryTimeSeconds : _afkTime >= Game.Settings.AfkTimeSeconds;

					if (shouldKick && isAfkEligible && Game.Settings.IdleKickEnabled)
					{
						Reject("reject_afk", $"Inactive {Game.Settings.IdleKickTimeSeconds}s");
						continue;
					}

					if (Player.IsAfk != afk)
					{
						if (afk && isAfkEligible && Game.Settings.AfkEnabled)
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
				_inactiveTime = _inactiveTime < int.MaxValue ? _inactiveTime + 1 : int.MaxValue;
				_afkTime = _afkTime < int.MaxValue ? _afkTime + 1 : int.MaxValue;
			}
		}

		private void CreatePlayer()
		{
			Player = Game.CreatePlayer(GetCookie("name"), false, GetCookie("player_token"));

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
			SendSkipNotification(skippedCard.Id, replacementCard.Id);
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
			if (Player.IsAfk && currentStage == GameStage.RoundInProgress)
			{
				_afkRecovery = true;
				SetAfkState(false);
				ResetIdleTime(true);
			}
			else if (oldStage == GameStage.GameStarting && currentStage == GameStage.RoundInProgress)
			{
				_afkRecovery = false;
				SetAfkState(false);
				ResetIdleTime(true);
			}
		}

		private void SendHandToPlayer()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_hand",
				blanks = Player.RemainingBlankCards,
				hand = Player.GetCurrentHand().Select(c => c.Id),
				discards = Player.Discards
			});
		}

		private void SendClientInfoToPlayer()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_clientinfo",
				player_id = Player.Id,
				player_name = Player.Name,
				player_token = Player.Token
			});
		}

		private void SendSelectionToPlayer()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_cardsplayed",
				selection = Player.GetSelectedCards().Select(c => c.Id)
			});
		}

		private void SendAuxDataToPlayer()
		{
			if (!IsOpen) return;
			SendMessageObject(new
			{
				msg = "s_auxclientdata",
				aux_points = Player.Coins
			});
		}

		protected override void OnOpen()
		{
			lock (_createDestroySync)
			{
				base.OnOpen();

				if (!IsOpen) return;

				// Make sure player can actually join
				if (Game.PlayerCount >= Game.Settings.MaxPlayers)
				{
					Reject(RejectCodes.ServerFull);
					return;
				}

				CreatePlayer();

				if (Game.Settings.AfkEnabled || Game.Settings.IdleKickEnabled)
				{
					ResetIdleTime(false);
					_idleCheckThread.Start();
				}

				Console.WriteLine($"{Player} ({GetIpAddress()}) connected");
			}
		}

		protected override void OnClose(CloseEventArgs e)
		{
			lock (_createDestroySync)
			{
				base.OnClose(e);				

				UnregisterEvents();

				var closeReason = e.Reason;
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

				var shouldPreserve = !IsRejected;

				if (Game.RemovePlayer(Player, closeReason, shouldPreserve))
				{
					Console.WriteLine($"{Player} disconnected: {closeReason} (code {e.Code})");
				}
			}
		}

		// Used when player is manually disconnected by server
		internal void NotifyRemoval(string reason)
		{
			if (!IsOpen) return;
			Console.WriteLine($"Player {Player} disconnected by server ({reason})");
			Context.WebSocket.Close();
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			base.OnMessage(e);
			var json = JToken.Parse(e.Data) as JObject;

			var msg = json?["msg"]?.Value<string>();
			if (msg == null) return;

			switch (msg)
			{
				case "c_updateinfo":
				{
					if (!(json["userinfo"] is JObject userInfoObject)) break;
					foreach (var (key, value) in userInfoObject)
					{
						switch (key.ToLowerInvariant())
						{
							case "name":
							{
								var strName = value.Value<string>();
								if (strName != Player.Name)
								{
									var oldName = Player.Name;
									var name = Game.CreatePlayerName(strName);
									Player.Name = name;
									Console.WriteLine($"{oldName} changed their name to {name}");
								}
								break;
							}
						}
					}
					ResetIdleTime(false);
					break;
				}
				case "c_playcards":
				{
					var cardArray = (json["cards"] as JArray)?
						.Select(v => Game.GetCardById(v.Value<string>()))
						.OfType<WhiteCard>()
						.ToArray();

					if (cardArray == null) break;
					Player.PlayCards(cardArray);
					ResetIdleTime(false);
					break;
				}
				case "c_judgecards":
				{
					var winningPlayIndex = json["play_index"]?.Value<int>() ?? -1;
					if (winningPlayIndex < 0) break;
					Player.JudgeCards(winningPlayIndex);
					ResetIdleTime(false);
					break;
				}
				case "c_upgradecard":
				{
					var requestedUpgradeCard = Game.GetCardById(json["card_id"]?.Value<string>()) as WhiteCard;
					Player.UpgradeCard(requestedUpgradeCard);
					ResetIdleTime(false);
					break;
				}
				case "c_discardcard":
				{
					var requestedDiscardCard = Game.GetCardById(json["card_id"]?.Value<string>()) as WhiteCard;
					Player.DiscardCard(requestedDiscardCard);
					ResetIdleTime(false);
					break;
				}
				case "c_vote_skip":
				{
					var voted = json["voted"]?.Value<bool>() ?? false;
					Player.SetSkipVoteState(voted);
					ResetIdleTime(false);
					break;
				}
				case "c_chat_msg":
				{
					Server.SendChatMessage(Player, json["body"].Value<string>());
					ResetIdleTime(false);
					break;
				}
			}
		}

		protected override void SendMessageObject(object o)
		{
			lock(_createDestroySync)
			{
				base.SendMessageObject(o);
			}
		}
	}
}
