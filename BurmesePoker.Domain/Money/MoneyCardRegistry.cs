using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Money;

/// <summary>
/// Which card values pay, and how much, for one round (RULES.md §4.1, §4.2).
/// </summary>
/// <remarks>
/// <para>
/// A registry is a <b>pure function of the round's turned-up cards</b> and nothing else
/// (BUILD-PLAN §3.3). Re-designating for a new round means constructing a new registry, so
/// designation is idempotent by construction — the retired implementation instead mutated a
/// <c>MoneyCardStatus</c> field on each <see cref="Card"/>, which double-marked on a second
/// pass and lost the marking whenever a card was copied. Nothing here writes to a card.
/// </para>
/// <para>
/// Designation is by <b>value</b>, via <see cref="Card.SameValueAs"/>: exact rank <i>and</i>
/// suit, so a turned-up 5♥ designates the two 5♥ copies in the shoe and no other five
/// (RULES.md §4.2). For a joker <c>SameValueAs</c> also discriminates on colour, so a
/// turned-up red joker designates the two red jokers and neither black one — §4.2 applied
/// unchanged, which is the recorded default for the open question at RULES.md §9 #11.
/// </para>
/// </remarks>
public sealed class MoneyCardRegistry
{
    /// <summary>
    /// 7♦ and A♠ pay in every round, in both decks (RULES.md §4.1). These are value
    /// designators only, never dealt: their ids are negative so that a stray comparison
    /// against a real card by <c>==</c> can only ever be false.
    /// </summary>
    private static readonly Card[] Permanent =
    [
        Card.Ranked(new CardId(-1), Rank.Seven, Suit.Diamonds),
        Card.Ranked(new CardId(-2), Rank.Ace, Suit.Spades)
    ];

    private readonly Card[] _turnedUp;

    /// <param name="turnedUp">
    /// The cards turned up at setup — normally two (RULES.md §3 step 4), but any number is
    /// accepted, including none, which leaves only the permanent money cards designated.
    /// The list is copied.
    /// </param>
    public MoneyCardRegistry(IReadOnlyList<Card> turnedUp)
    {
        ArgumentNullException.ThrowIfNull(turnedUp);
        _turnedUp = [.. turnedUp];
    }

    /// <summary>
    /// What the given card pays its owner, as a multiple of the money card value:
    /// <c>0</c> for an ordinary card, <c>1</c> for a money card, <c>2</c> for a double.
    /// </summary>
    /// <remarks>
    /// Doubling is the overlap of the two ways a card can be designated, and it is the
    /// ceiling — turning up both copies of the 5♥ still pays once, and turning up both
    /// copies of the 7♦ still pays twice (RULES.md §4.1).
    /// </remarks>
    public int Multiplier(Card card)
    {
        var permanent = Array.Exists(Permanent, designator => card.SameValueAs(designator));
        var designated = Array.Exists(_turnedUp, designator => card.SameValueAs(designator));

        return (permanent ? 1 : 0) + (designated ? 1 : 0);
    }
}
