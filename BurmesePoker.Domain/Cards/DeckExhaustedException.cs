namespace BurmesePoker.Domain.Cards;

/// <summary>
/// Thrown when a card is drawn from a deck that has none left. A domain exception rather
/// than the <see cref="InvalidOperationException"/> a bare LINQ call would raise, so a
/// caller can tell "the draw pile ran out" — a real game situation (RULES.md §5) — apart
/// from an ordinary programming error.
/// </summary>
public sealed class DeckExhaustedException : Exception
{
    public DeckExhaustedException()
        : base("The deck is empty; there is no card to draw.")
    {
    }

    public DeckExhaustedException(string message) : base(message)
    {
    }

    public DeckExhaustedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
