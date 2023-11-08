namespace BurmesePoker;

internal class CardPlay(CardPlayType type, IEnumerable<Card> cards)
{
    internal CardPlayType Type { get; } = type;
    internal IEnumerable<Card> Cards { get; } = cards;
}
