namespace BurmesePoker.Domain.Cards;

/// <summary>
/// Display glyphs, display codes and sort ordering for ranks and suits. Salvaged from the
/// retired 2023 <c>Common.cs</c> (BUILD-PLAN §1.2) and re-expressed against rankless,
/// suitless jokers: a <c>null</c> rank or suit is a joker.
/// </summary>
/// <remarks>
/// This type is text and ordering only. It holds no rules — rank adjacency, ace handling
/// and meld validity all live with the meld generators (RULES.md §6).
/// </remarks>
public static class CardText
{
    /// <summary>The four suits, in the display order used by the 2023 front end.</summary>
    public static IReadOnlyList<Suit> AllSuits { get; } = new[]
    {
        Suit.Hearts,
        Suit.Spades,
        Suit.Clubs,
        Suit.Diamonds
    };

    /// <summary>The thirteen ranks, low to high.</summary>
    public static IReadOnlyList<Rank> AllRanks { get; } = new[]
    {
        Rank.Two,
        Rank.Three,
        Rank.Four,
        Rank.Five,
        Rank.Six,
        Rank.Seven,
        Rank.Eight,
        Rank.Nine,
        Rank.Ten,
        Rank.Jack,
        Rank.Queen,
        Rank.King,
        Rank.Ace
    };

    /// <summary>The colour a suit is printed in.</summary>
    public static CardColor ColorOf(Suit suit) => suit switch
    {
        Suit.Hearts => CardColor.Red,
        Suit.Diamonds => CardColor.Red,
        Suit.Spades => CardColor.Black,
        Suit.Clubs => CardColor.Black,
        _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, "Not a valid suit.")
    };

    /// <summary>
    /// Sort position of a rank: 0 for Two through 12 for Ace, and 13 for a joker
    /// (<c>null</c>), which sorts last.
    /// </summary>
    public static int Order(Rank? rank) => rank is null ? 13 : (int)rank.Value - 2;

    /// <summary>The short display code for a rank — "10", "J", "A", or the joker glyph.</summary>
    public static string DisplayCode(Rank? rank) => rank switch
    {
        null => "🃏",
        Rank.Two => "2",
        Rank.Three => "3",
        Rank.Four => "4",
        Rank.Five => "5",
        Rank.Six => "6",
        Rank.Seven => "7",
        Rank.Eight => "8",
        Rank.Nine => "9",
        Rank.Ten => "10",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        Rank.Ace => "A",
        _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, "Not a valid rank.")
    };

    /// <summary>The suit glyph, or the empty string for a joker (<c>null</c>).</summary>
    public static string DisplaySuit(Suit? suit) => suit switch
    {
        null => string.Empty,
        Suit.Hearts => "♥",
        Suit.Spades => "♠",
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, "Not a valid suit.")
    };

    /// <summary>
    /// Parses a rank code — "2".."10", "T", "J", "Q", "K", "A", case-insensitive. Jokers
    /// have no rank and so cannot be parsed here.
    /// </summary>
    public static Rank ParseRank(string code) => code?.ToUpperInvariant() switch
    {
        "2" => Rank.Two,
        "3" => Rank.Three,
        "4" => Rank.Four,
        "5" => Rank.Five,
        "6" => Rank.Six,
        "7" => Rank.Seven,
        "8" => Rank.Eight,
        "9" => Rank.Nine,
        "10" => Rank.Ten,
        "T" => Rank.Ten,
        "J" => Rank.Jack,
        "Q" => Rank.Queen,
        "K" => Rank.King,
        "A" => Rank.Ace,
        _ => throw new ArgumentException($"'{code}' is not a valid rank code.", nameof(code))
    };
}
