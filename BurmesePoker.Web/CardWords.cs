using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Web;

/// <summary>
/// A card said out loud — <c>7♦</c> as <em>"seven of diamonds"</em>.
/// </summary>
/// <remarks>
/// <para>
/// <b>A glyph is not a word.</b> A screen reader reading <c>7♦</c> says whatever its symbol
/// table happens to say, which for the suit glyphs ranges from "black diamond suit" to nothing
/// at all. So every card on the page carries the glyph for the eye, hidden from the
/// accessibility tree, and this text for the ear — the same information twice rather than a
/// picture and a shrug (BUILD-PLAN §3.11, "done when: you can watch a whole round with a
/// screen reader").
/// </para>
/// <para>
/// It is a browser's problem and lives with the browser. The console has no equivalent and
/// needs none, and <c>CardText</c> in the domain is glyphs and ordering (P1) — a spoken name
/// is neither.
/// </para>
/// </remarks>
public static class CardWords
{
    /// <summary>The card as a phrase: <em>"seven of diamonds"</em>, <em>"red joker"</em>.</summary>
    public static string For(Card card) => card.IsJoker
        ? $"{Colour(card.Color)} joker"
        : $"{Of(card.Rank!.Value)} of {Of(card.Suit!.Value)}";

    private static string Colour(CardColor colour) => colour switch
    {
        CardColor.Red => "red",
        _ => "black"
    };

    private static string Of(Rank rank) => rank switch
    {
        Rank.Two => "two",
        Rank.Three => "three",
        Rank.Four => "four",
        Rank.Five => "five",
        Rank.Six => "six",
        Rank.Seven => "seven",
        Rank.Eight => "eight",
        Rank.Nine => "nine",
        Rank.Ten => "ten",
        Rank.Jack => "jack",
        Rank.Queen => "queen",
        Rank.King => "king",
        Rank.Ace => "ace",
        _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, "Not a valid rank.")
    };

    private static string Of(Suit suit) => suit switch
    {
        Suit.Hearts => "hearts",
        Suit.Spades => "spades",
        Suit.Clubs => "clubs",
        Suit.Diamonds => "diamonds",
        _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, "Not a valid suit.")
    };
}
