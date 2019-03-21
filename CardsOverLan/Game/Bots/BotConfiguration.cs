using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardsOverLan.Game.Bots
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public sealed class BotConfiguration
	{
		private const int DefaultPlayMaxBaseDelay = 2000;
		private const int DefaultPlayMinBaseDelay = 8000;
		private const int DefaultPlayMinDelayPerCard = 3000;
		private const int DefaultPlayMaxPerCardDelay = 4000;
		private const int DefaultJudgeMinPerPlayDelay = 3000;
		private const int DefaultJudgeMaxPerPlayDelay = 5500;
		private const int DefaultJudgeMinPerCardDelay = 2000;
		private const int DefaultJudgeMaxPerCardDelay = 3000;
		private const int DefaultMinTypingInterval = 40;
		private const int DefaultMaxTypingInterval = 55;
		private const int DefaultMinTypingDelay = 750;
		private const int DefaultMaxTypingDelay = 2000;

		[JsonProperty("play_max_base_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultPlayMaxBaseDelay)]
		public int PlayMaxBaseDelay { get; set; } = DefaultPlayMaxBaseDelay;

		[JsonProperty("play_min_base_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultPlayMinBaseDelay)]
		public int PlayMinBaseDelay { get; set; } = DefaultPlayMinBaseDelay;

		[JsonProperty("play_min_per_card_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultPlayMinDelayPerCard)]
		public int PlayMinPerCardDelay { get; set; } = DefaultPlayMinDelayPerCard;

		[JsonProperty("play_max_per_card_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultPlayMaxPerCardDelay)]
		public int PlayMaxPerCardDelay { get; set; } = DefaultPlayMaxPerCardDelay;

		[JsonProperty("judge_min_per_play_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultJudgeMinPerPlayDelay)]
		public int JudgeMinPerPlayDelay { get; set; } = DefaultJudgeMinPerPlayDelay;

		[JsonProperty("judge_max_per_play_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultJudgeMaxPerPlayDelay)]
		public int JudgeMaxPerPlayDelay { get; set; } = DefaultJudgeMaxPerPlayDelay;

		[JsonProperty("judge_min_per_card_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultJudgeMinPerCardDelay)]
		public int JudgeMinPerCardDelay { get; set; } = DefaultJudgeMinPerCardDelay;

		[JsonProperty("judge_max_per_card_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultJudgeMaxPerCardDelay)]
		public int JudgeMaxPerCardDelay { get; set; } = DefaultJudgeMaxPerCardDelay;

		[JsonProperty("min_typing_interval", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultMinTypingInterval)]
		public int MinTypingInterval { get; set; } = DefaultMinTypingInterval;

		[JsonProperty("max_typing_interval", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultMaxTypingInterval)]
		public int MaxTypingInterval { get; set; } = DefaultMaxTypingInterval;

		[JsonProperty("min_typing_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultMinTypingDelay)]
		public int MinTypingDelay { get; set; } = DefaultMinTypingDelay;

		[JsonProperty("max_typing_delay", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		[DefaultValue(DefaultMaxTypingDelay)]
		public int MaxTypingDelay { get; set; } = DefaultMaxTypingDelay;
	}
}
