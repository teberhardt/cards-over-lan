using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace CardsOverLan.Game.Bots
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class Taunt
	{
		[JsonProperty("trigger_content", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.DisallowNull)]
		private readonly HashList<string> _triggerContent = new HashList<string>();
		[JsonProperty("trigger_cards", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.DisallowNull)]
		private readonly HashList<string> _triggerCards = new HashList<string>();
		[JsonProperty("responses", DefaultValueHandling = DefaultValueHandling.Ignore, Required = Required.Always)]
		private readonly HashList<LocalizedString> _responses = new HashList<LocalizedString>();

		public Taunt()
		{
		}

		[JsonProperty("response_chance", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(1.0)]
		public double ResponseChance { get; set; } = 1.0;

		[JsonProperty("trigger_event", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, Required = Required.DisallowNull)]
		[DefaultValue("")]
		public string TriggerEvent { get; set; } = "";

		[JsonProperty("priority", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(1)]
		public int Priority { get; set; } = 1;

		public bool IsPlayEligible(IEnumerable<WhiteCard> play)
		{
			return play.Any(c => _triggerCards.Contains(c.ID)) || _triggerContent.Any(tc => play.Any(p => p.ContainsContentFlags(tc)));
		}

		public bool AddContentTrigger(string content)
		{
			lock(_triggerContent)
			{
				if (string.IsNullOrWhiteSpace(content)) return false;
				return _triggerContent.Add(content.Trim().ToLowerInvariant());
			}
		}

		public bool RemoveContentTrigger(string content)
		{
			lock(_triggerContent)
			{
				if (string.IsNullOrWhiteSpace(content)) return false;
				return _triggerContent.Remove(content.Trim().ToLowerInvariant());
			}
		}

		public bool AddCardTrigger(string cardId)
		{
			lock(_triggerCards)
			{
				if (string.IsNullOrWhiteSpace(cardId)) return false;
				return _triggerCards.Add(cardId.Trim().ToLowerInvariant());
			}
		}

		public bool RemoveCardTrigger(string cardId)
		{
			lock(_triggerCards)
			{
				if (string.IsNullOrWhiteSpace(cardId)) return false;
				return _triggerCards.Remove(cardId.Trim().ToLowerInvariant());
			}
		}

		public bool AddResponse(LocalizedString response)
		{
			lock(_responses)
			{
				if (response == null) return false;
				return _responses.Add(response);
			}
		}

		public bool RemoveResponse(LocalizedString response)
		{
			lock(_responses)
			{
				if (response == null) return false;
				return _responses.Remove(response);
			}
		}

		public void ClearCardTriggers()
		{
			lock (_triggerCards)
			{
				_triggerCards.Clear();
			}
		}

		public void ClearContentTriggers()
		{
			lock (_triggerContent)
			{
				_triggerContent.Clear();
			}
		}

		public void ClearResponses()
		{
			lock(_responses)
			{
				_responses.Clear();
			}
		}

		public IEnumerable<string> GetContentTriggers()
		{
			lock(_triggerContent)
			{
				foreach (var content in _triggerContent)
				{
					yield return content;
				}
			}
		}

		public IEnumerable<string> GetCardTriggers()
		{
			lock(_triggerCards)
			{
				foreach (var id in _triggerCards)
				{
					yield return id;
				}
			}
		}

		public IEnumerable<LocalizedString> GetResponses()
		{
			lock(_responses)
			{
				foreach(var response in _responses)
				{
					yield return response;
				}
			}
		}
	}
}
