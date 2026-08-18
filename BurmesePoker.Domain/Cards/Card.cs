namespace BurmesePoker.Domain.Cards;

/// <summary>
/// One physical card out of the two-deck shoe. A joker carries <c>null</c> for both
/// <see cref="Rank"/> and <see cref="Suit"/> and is told apart from the other joker of its
/// deck by <see cref="Color"/> (BUILD-PLAN §3.2).
/// </summary>
/// <remarks>
/// <para>
/// The two identity notions are deliberately both explicit (BUILD-PLAN §3.1). As a
/// <c>record struct</c>, equality <b>includes <see cref="Id"/></b>, so <c>==</c> is
/// <i>instance</i> identity: the two copies of 5♥ are not equal. Value identity — the
/// question money-card designation asks — is the separately named
/// <see cref="SameValueAs"/>.
/// </para>
/// <para>
/// Prefer <see cref="Ranked"/> and <see cref="Joker"/> over the positional constructor:
/// they derive <see cref="Color"/> from the suit, so a card cannot be built with a colour
/// that contradicts its suit.
/// </para>
/// </remarks>
public readonly record struct Card(CardId Id, Rank? Rank, Suit? Suit, CardColor Color)
{
    /// <summary>A joker is the rankless, suitless card. Rank and suit are always null together.</summary>
    public bool IsJoker => Rank is null;

    /// <summary>
    /// Value identity: same rank, same suit, same colour — ignoring <see cref="Id"/>.
    /// This is the comparison money-card designation uses (RULES.md §4.2), and it is
    /// deliberately <i>not</i> <c>==</c>.
    /// </summary>
    public bool SameValueAs(Card other) =>
        Rank == other.Rank && Suit == other.Suit && Color == other.Color;

    /// <summary>Builds a ranked card, taking its colour from its suit.</summary>
    public static Card Ranked(CardId id, Rank rank, Suit suit) =>
        new(id, rank, suit, CardText.ColorOf(suit));

    /// <summary>Builds a joker of the given colour — rankless and suitless.</summary>
    public static Card Joker(CardId id, CardColor color) => new(id, null, null, color);

    public override string ToString() =>
        IsJoker ? $"🃏{Color}" : $"{CardText.DisplayCode(Rank)}{CardText.DisplaySuit(Suit)}";
}
