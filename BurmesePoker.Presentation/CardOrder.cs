using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Presentation;

/// <summary>
/// The order a hand is shown in: hearts, spades, clubs, diamonds, low to high, jokers last.
/// </summary>
/// <remarks>
/// <para>
/// <b>It is a display decision, so it lives in the presentation layer</b> — the domain hands
/// back cards in whatever order the deal or the search produced, and deliberately makes no
/// promise about it. The order itself comes from the salvaged <see cref="CardText.AllSuits"/>,
/// so a hand reads the way the 2023 front end showed it (BUILD-PLAN P8).
/// </para>
/// <para>
/// <b>Total, and it has to be.</b> Two decks mean a hand can hold two cards of the same value
/// (BUILD-PLAN §3.1); ties break on colour and then on <see cref="CardId"/>, so the same hand
/// always lays out the same way and a front end re-sorting after a draw does not shuffle
/// identical-looking cards past each other.
/// </para>
/// </remarks>
public static class CardOrder
{
    private static readonly Dictionary<Suit, int> SuitOrder = CardText.AllSuits
        .Select((suit, index) => (suit, index))
        .ToDictionary(entry => entry.suit, entry => entry.index);

    /// <summary>Cards in display order.</summary>
    public static IEnumerable<Card> Display(IEnumerable<Card> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        return cards
            .OrderBy(card => card.Suit is null ? CardText.AllSuits.Count : SuitOrder[card.Suit.Value])
            .ThenBy(card => CardText.Order(card.Rank))
            .ThenBy(card => card.Color)
            .ThenBy(card => card.Id.Value);
    }
}
