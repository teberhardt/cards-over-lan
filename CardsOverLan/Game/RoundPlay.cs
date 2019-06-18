using System.Collections.Generic;
using System.Linq;

namespace CardsOverLan.Game
{
    public sealed class RoundPlay
    {
        private readonly WhiteCard[] _whiteCards;

        public BlackCard PromptCard { get; }

        public Player Player { get; }

        public bool Winning { get; internal set; }

        public IEnumerable<WhiteCard> GetCards()
        {
            return _whiteCards;
        }

        internal RoundPlay(Player player, IEnumerable<WhiteCard> whiteCards, BlackCard prompt)
        {
            Player = player;
            _whiteCards = whiteCards.ToArray();
            PromptCard = prompt;
        }
    }
}