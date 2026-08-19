using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// Everything on the table for one round: the seating, the draw pile, the turned-up money
/// cards, and the two records the money layer settles from.
/// </summary>
/// <remarks>
/// <para>
/// Built by <see cref="RoundEngine"/> at setup and mutated by nothing else — the draw pile
/// and the seats are reachable only through the engine, so a front end can read the table
/// but cannot play from it.
/// </para>
/// <para>
/// <b><see cref="Shoe"/> is the unshuffled shoe, and settlement needs exactly that.</b>
/// <see cref="Money.Settlement.ForRound"/> resolves an owned <see cref="CardId"/> back to a
/// <see cref="Card"/> by index and rejects any list that is not index-aligned, so the
/// shuffled <see cref="Deck.Cards"/> would throw. The engine keeps the builder list the
/// round was dealt from and hands that over.
/// </para>
/// </remarks>
public sealed class TableState
{
    private readonly List<PlayerState> _seats;
    private readonly Dictionary<PlayerId, PlayerState> _seatsById;
    private readonly List<Card> _turnedUpOnTable;

    internal TableState(
        IReadOnlyList<PlayerId> players,
        Stakes stakes,
        Deck drawPile,
        IReadOnlyList<Card> shoe,
        Card turnedUpFromBottom,
        Card turnedUpFromTop)
    {
        Players = [.. players];
        Stakes = stakes;
        DrawPile = drawPile;
        Shoe = shoe;
        TurnedUpFromBottom = turnedUpFromBottom;
        TurnedUpFromTop = turnedUpFromTop;

        _seats = [.. Players.Select(player => new PlayerState(player))];
        _seatsById = _seats.ToDictionary(seat => seat.Id);
        _turnedUpOnTable = [turnedUpFromBottom, turnedUpFromTop];

        // Designation is fixed at setup and is a pure function of the two cards turned up
        // (RULES.md §3 step 5, §4.2) — including the top one, whether or not the opening
        // player later claims the physical card away (RULES.md §9 #12).
        MoneyCards = new MoneyCardRegistry(_turnedUpOnTable);
    }

    /// <summary>The seating order this round, which is also the turn order (RULES.md §3).</summary>
    public IReadOnlyList<PlayerId> Players { get; }

    /// <summary>Every seat, in seating order.</summary>
    public IReadOnlyList<PlayerState> Seats => _seats;

    /// <summary>The stakes this round is played for. Fixed for the whole match.</summary>
    public Stakes Stakes { get; }

    /// <summary>The 108 cards of the shoe, unshuffled and index-aligned, for settlement.</summary>
    public IReadOnlyList<Card> Shoe { get; }

    /// <summary>The card turned up from the <b>bottom</b> of the deck (RULES.md §3 step 4).</summary>
    public Card TurnedUpFromBottom { get; }

    /// <summary>
    /// The card turned up from the <b>top</b> of the deck — the one the opening player may
    /// claim instead of drawing (RULES.md §4.5).
    /// </summary>
    public Card TurnedUpFromTop { get; }

    /// <summary>Whether the opening player took the top money card.</summary>
    public bool TurnedUpFromTopClaimed { get; private set; }

    /// <summary>
    /// The turned-up cards still lying on the table — two, or one after a claim. What they
    /// <em>designate</em> does not change when one is claimed; only the physical card moves.
    /// </summary>
    public IReadOnlyList<Card> TurnedUpOnTable => _turnedUpOnTable;

    /// <summary>Which card values pay this round, and how much (RULES.md §4).</summary>
    public MoneyCardRegistry MoneyCards { get; }

    /// <summary>Who the deck gave each card to. Append-only, and never consulted for a hand.</summary>
    public CardOwnership Ownership { get; } = new();

    /// <summary>How many cards are left to draw.</summary>
    public int DrawPileCount => DrawPile.Count;

    /// <summary>
    /// Every card in the round, wherever it currently sits: draw pile, hands, discard piles,
    /// and the turned-up cards still on the table.
    /// </summary>
    /// <remarks>
    /// The invariant is that this is always the whole shoe — 108 distinct cards. The 2023
    /// implementation broke it by cloning the claimed money card into a 109th
    /// (RULES-TECHNICAL.md §5); here the claim <em>moves</em> the card.
    /// </remarks>
    public IEnumerable<Card> AllCards =>
        DrawPile.Cards
            .Concat(_seats.SelectMany(seat => seat.Hand))
            .Concat(_seats.SelectMany(seat => seat.Discards))
            .Concat(_turnedUpOnTable);

    /// <summary>The seat belonging to the given player.</summary>
    /// <exception cref="ArgumentException">Nobody of that name is at this table.</exception>
    public PlayerState SeatOf(PlayerId player) =>
        _seatsById.TryGetValue(player, out var seat)
            ? seat
            : throw new ArgumentException($"{player} is not at this table.", nameof(player));

    internal Deck DrawPile { get; private set; }

    /// <summary>
    /// Gathers every discard pile, shuffles it, and makes it the new draw pile (RULES.md §5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The turned-up cards are not gathered.</b> They stay on the table for the rest of the
    /// round — RULES.md §9 #4's recommendation, taken as the default.
    /// </para>
    /// <para>
    /// Every pile is swept, which is safe because only the previous player's top discard is
    /// ever takeable and this runs at the moment a player draws, by which point that offer has
    /// been declined (RULES.md §9 #9). Cards move from the piles into the new deck in one step,
    /// so an observer looking between engine steps still sees all 108.
    /// </para>
    /// </remarks>
    /// <returns>How many cards were gathered. Zero means there is genuinely nothing left.</returns>
    internal int ReplaceDrawPileWithTheDiscards(Random random)
    {
        if (!DrawPile.IsEmpty)
        {
            throw new InvalidOperationException(
                "The discards are gathered when the draw pile runs out, not before (RULES.md §5).");
        }

        var gathered = new List<Card>();

        foreach (var seat in _seats)
        {
            gathered.AddRange(seat.TakeAllDiscards());
        }

        var replacement = new Deck(gathered);
        replacement.Shuffle(random);
        DrawPile = replacement;
        return gathered.Count;
    }

    /// <summary>
    /// Takes the top money card off the table (RULES.md §4.5). The <b>actual</b> card leaves
    /// the table — it is never copied — and the caller records no ownership for it, because
    /// the deck did not give it to anybody (RULES.md §4.4).
    /// </summary>
    internal Card ClaimTurnedUpFromTop()
    {
        if (TurnedUpFromTopClaimed)
        {
            throw new InvalidOperationException("The top money card has already been claimed.");
        }

        TurnedUpFromTopClaimed = true;
        _turnedUpOnTable.Remove(TurnedUpFromTop);
        return TurnedUpFromTop;
    }
}
