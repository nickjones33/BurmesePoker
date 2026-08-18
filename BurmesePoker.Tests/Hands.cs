using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Tests;

/// <summary>
/// Builds hands from short codes so meld tests read like the rules do.
/// </summary>
/// <remarks>
/// A code is a rank code followed by a suit letter — <c>2D</c>, <c>10H</c>, <c>AS</c> — or
/// <c>RJ</c> / <c>BJ</c> for a red or black joker. Cards are given sequential
/// <see cref="CardId"/>s in the order listed, so a test can name a card by its position and
/// still tell two copies of the same card apart.
/// </remarks>
internal static class Hands
{
    public static IReadOnlyList<Card> Of(params string[] codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        return [.. codes.Select((code, index) => Parse(code, new CardId(index)))];
    }

    private static Card Parse(string code, CardId id) => code.ToUpperInvariant() switch
    {
        "RJ" => Card.Joker(id, CardColor.Red),
        "BJ" => Card.Joker(id, CardColor.Black),
        var ranked => Card.Ranked(
            id,
            CardText.ParseRank(ranked[..^1]),
            SuitOf(ranked[^1]))
    };

    private static Suit SuitOf(char letter) => letter switch
    {
        'H' => Suit.Hearts,
        'S' => Suit.Spades,
        'C' => Suit.Clubs,
        'D' => Suit.Diamonds,
        _ => throw new ArgumentException($"'{letter}' is not a suit letter.", nameof(letter))
    };
}
