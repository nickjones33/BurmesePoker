using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

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
/// unchanged, which is RULES.md §9 #11's answer.
/// </para>
/// <para>
/// 🔥 <b>The design decision has not been abandoned; its inputs have widened</b> (RULES.md
/// §10 #17). <see cref="Multiplier(Card)"/> still answers about a card in isolation, because
/// what a <i>value</i> pays is still a pure function of the turn-up. What is new is the
/// jackpot: one player owning <b>both</b> partners of a 7♦/A♠ turn-up is paid ×5 apiece where
/// two players holding one each are paid ×3 (§4.1), so the whole answer needs the round's
/// ownership as well. It is still computed and still never stored — see
/// <see cref="MoneyOwnership"/>.
/// </para>
/// </remarks>
public sealed class MoneyCardRegistry
{
    /// <summary>
    /// What a value pays when a designation lands on a card that already pays (RULES.md §4.1).
    /// </summary>
    /// <remarks>
    /// <b>Three is the number that conserves what a designation is worth</b>, which is why it
    /// is three and not two: the shown copy can never be owned (§4.4), so a designation on the
    /// 7♦ leaves one payable copy where an ordinary designation leaves one payable partner
    /// <i>and</i> both 7♦s alone. 1 for the partner's own permanence, 1 for the designation,
    /// and 1 inherited from the copy nobody can be paid for.
    /// </remarks>
    public const int Tripled = 3;

    /// <summary>
    /// What the two partners of a 7♦/A♠ turn-up pay each when one player owns both
    /// (RULES.md §4.1, §4.3) — ten money-card values to a single player, against a round
    /// prize of five. The largest single swing in the game.
    /// </summary>
    public const int Jackpot = 5;

    /// <summary>
    /// 7♦, A♠ and every joker pay in every round, in both decks (RULES.md §4.1). These are
    /// value designators only, never dealt: their ids are negative so that a stray comparison
    /// against a real card by <c>==</c> can only ever be false.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Eight cards, not four</b> — two 7♦, two A♠ and <b>four jokers</b> (rev 21). A
    /// joker's identity is its colour and <see cref="Card.SameValueAs"/> discriminates on it,
    /// so both colours have to be listed and neither needs a special case. Doubling the
    /// permanent side-bet is what moves every figure derived from its size (§4.3, §4.4).
    /// </remarks>
    private static readonly Card[] Permanent =
    [
        Card.Ranked(new CardId(-1), Rank.Seven, Suit.Diamonds),
        Card.Ranked(new CardId(-2), Rank.Ace, Suit.Spades),
        Card.Joker(new CardId(-3), CardColor.Red),
        Card.Joker(new CardId(-4), CardColor.Black)
    ];

    /// <summary>The two values the ×5 is stated of, and no others (RULES.md §9 #32).</summary>
    private static readonly Card[] JackpotPair = [Permanent[0], Permanent[1]];

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
    /// What the given card's <em>value</em> pays its owner, as a multiple of the money card
    /// value: <c>0</c> for an ordinary card, <c>1</c> for a money card, <see cref="Tripled"/>
    /// for a designation that lands on a permanent one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The triple is the overlap of the two ways a value can pay, and it is the ceiling that
    /// designation alone can reach — turning up both copies of the 5♥ still pays once, and
    /// turning up both copies of the 7♦ still pays three times (RULES.md §4.1).
    /// </para>
    /// <para>
    /// ⚠️ <b>This is the answer for a card considered on its own, and the jackpot is not a
    /// property of a card</b> — use <see cref="Multiplier(Card, PlayerId, MoneyOwnership)"/>
    /// where the answer has to be paid out. A view that draws a marker beside a card wants
    /// this one.
    /// </para>
    /// </remarks>
    public int Multiplier(Card card)
    {
        var permanent = Array.Exists(Permanent, designator => card.SameValueAs(designator));
        var designated = Array.Exists(_turnedUp, designator => card.SameValueAs(designator));

        return (permanent, designated) switch
        {
            (true, true) => Tripled,
            (true, false) or (false, true) => 1,
            _ => 0
        };
    }

    /// <summary>
    /// What the given card pays <paramref name="owner"/>, as a multiple of the money card
    /// value: <c>0</c>, <c>1</c>, <see cref="Tripled"/> or <see cref="Jackpot"/>.
    /// </summary>
    /// <param name="card">The card, resolved to its value.</param>
    /// <param name="owner">The player the deck gave it to (RULES.md §4.4).</param>
    /// <param name="ownership">
    /// The round's ownership configuration, from <see cref="ConfigurationOf"/>. Pass
    /// <see cref="MoneyOwnership.None"/> to ask what the value pays with no jackpot in play.
    /// </param>
    /// <remarks>
    /// The jackpot lands on the tripled cards and only on them, and it can only be reached at
    /// all when the turn-up is the 7♦/A♠ pair — in which case those two values are the only
    /// tripled ones there are. So the test below is exactly as narrow as the rule, without a
    /// second look at the designators.
    /// </remarks>
    public int Multiplier(Card card, PlayerId owner, MoneyOwnership ownership)
    {
        var multiplier = Multiplier(card);

        return multiplier == Tripled && ownership.OwnsBothPartners == owner
            ? Jackpot
            : multiplier;
    }

    /// <summary>
    /// Reads the round's ownership for the one thing a multiplier needs from it: whether a
    /// single player owns both partners of a 7♦/A♠ turn-up (RULES.md §4.1).
    /// </summary>
    /// <param name="ownership">Who the deck gave each card to.</param>
    /// <param name="shoe">
    /// The 108 cards of the shoe in <see cref="DeckBuilder.BuildTwoDecks"/> order, which
    /// resolves an owned <see cref="CardId"/> back to a <see cref="Card"/>: ownership is
    /// recorded by instance and designation asks by value (BUILD-PLAN §3.1).
    /// </param>
    /// <remarks>
    /// <para>
    /// <b>Computed once a round, never once a card.</b> Settlement walks every ownership
    /// record; this walks the shoe once, before it starts.
    /// </para>
    /// <para>
    /// ⚠️ <b>The pair is the 7♦ and the A♠ and nothing else</b> (RULES.md §9 #32 — open).
    /// Two jokers of opposite colours, or a 7♦ and a joker, are two tripled values and are
    /// deliberately <b>not</b> recognised here.
    /// </para>
    /// </remarks>
    public MoneyOwnership ConfigurationOf(CardOwnership ownership, IReadOnlyList<Card> shoe)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(shoe);

        if (!TurnUpIsTheJackpotPair())
        {
            return MoneyOwnership.None;
        }

        PlayerId? holder = null;

        foreach (var designator in JackpotPair)
        {
            // The partner is the copy that is not lying on the table. A turned-up card can
            // never be owned (RULES.md §4.4), so an owner found here is always the partner's.
            PlayerId? partnersOwner = null;

            foreach (var card in shoe)
            {
                if (card.SameValueAs(designator) && !Array.Exists(_turnedUp, up => up == card))
                {
                    partnersOwner = ownership.OwnerOf(card.Id);
                    break;
                }
            }

            if (partnersOwner is null || (holder is not null && holder != partnersOwner))
            {
                return MoneyOwnership.None;
            }

            holder = partnersOwner;
        }

        return new MoneyOwnership(holder);
    }

    private bool TurnUpIsTheJackpotPair() =>
        _turnedUp.Length == 2
        && Array.TrueForAll(
            JackpotPair,
            designator => Array.Exists(_turnedUp, up => up.SameValueAs(designator)));
}
