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

    /// <summary>
    /// Rank identity: the same rank, whatever the suit — and, for a joker, the other jokers.
    /// This is the comparison the feeding ban asks (RULES.md §5.1), and it is deliberately
    /// neither <c>==</c> nor <see cref="SameValueAs"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The third identity notion, and the one trap in §5.1.</b> BUILD-PLAN §3.1 names two:
    /// <c>==</c> is instance identity and is what the exact-cover search uses;
    /// <see cref="SameValueAs"/> is rank <em>and</em> suit <em>and</em> colour and is what money
    /// designation uses, because §4.2 matches a turned-up 5♥ to only the two 5♥s. §5.1 matches on
    /// <b>neither</b>: taking the Q♦ closes off <em>every</em> Queen, so reaching for
    /// <see cref="SameValueAs"/> here would leave the Q♣ Mya Lay actually objected to perfectly
    /// legal.
    /// </para>
    /// <para>
    /// ⚠️ <b>A joker has no rank, and what falls out of that is a house ruling rather than a
    /// derivation</b> (RULES.md §9 #27, <c>PLAYER</c>). <see cref="Rank"/> is null for a joker, so
    /// nullable equality makes joker match joker and nothing else — which is the ruling *"taking a
    /// joker closes the other jokers"*. It is the one line here that is not <c>EXPERT</c>: if it is
    /// ever put to a player and comes back differently, this is a rule change and not a typo.
    /// </para>
    /// <para>
    /// ✅ <b>One predicate, two rules.</b> RULES.md §9 #30 settled that an objection to a money-card
    /// claim turns on the rank as well (§4.5), which is §5.1's own predicate — so the claim's test
    /// and the ban's test are this method and must not be written twice.
    /// </para>
    /// </remarks>
    public bool SameRankAs(Card other) => Rank == other.Rank;

    /// <summary>Builds a ranked card, taking its colour from its suit.</summary>
    public static Card Ranked(CardId id, Rank rank, Suit suit) =>
        new(id, rank, suit, CardText.ColorOf(suit));

    /// <summary>Builds a joker of the given colour — rankless and suitless.</summary>
    public static Card Joker(CardId id, CardColor color) => new(id, null, null, color);

    public override string ToString() =>
        IsJoker ? $"🃏{Color}" : $"{CardText.DisplayCode(Rank)}{CardText.DisplaySuit(Suit)}";
}
