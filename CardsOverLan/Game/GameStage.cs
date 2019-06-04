using CardsOverLan.Game.Converters;
using Newtonsoft.Json;

namespace CardsOverLan.Game
{
	[JsonConverter(typeof(EnumNameConverter))]
	public enum GameStage
	{
		/// <summary>
		/// Players are choosing cards.
		/// </summary>
		[Name("playing")]
		RoundInProgress,
		/// <summary>
		/// Winning play is being chosen.
		/// </summary>
		[Name("judging")]
		JudgingCards,
		/// <summary>
		/// Round is over and winning play is displayed.
		/// </summary>
		[Name("round_end")]
		RoundEnd,
		/// <summary>
		/// Game is over and winning player is displayed.
		/// </summary>
		[Name("game_end")]
		GameEnd,
		/// <summary>
		/// Game is waiting to begin.
		/// </summary>
		[Name("game_starting")]
		GameStarting
	}
}
