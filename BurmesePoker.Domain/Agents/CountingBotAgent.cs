using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="CautiousBotAgent"/> with a memory: it estimates what is still in the shoe from
/// everything it has been shown this round, rather than from its own thirteen cards
/// (BUILD-PLAN P20).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>One substitution, and it is <see cref="ThreatScore.Supply"/>.</b> Every decision this
/// rung makes is the rung below's, arithmetic included; the only thing that differs is the
/// answer to <em>how many copies of that card are left</em>. Cautious answers it from its hand
/// and so counts a card it threw away four turns ago as available again; this one does not.
/// P15's discipline is that a rung differs from the one below it in exactly one decision, or a
/// difference in results attributes to nothing — and that is a supply estimate, not a strategy.
/// </para>
/// <para>
/// ⚠️ <b>The information set was decided before the bot was written, and it is the cautious
/// default.</b> RULES.md §9 #15 — <em>is a discard pile inspectable, or only its top card?</em>
/// — is open, and P20 must not answer it in code. So this seat counts <b>only what it has
/// actually been shown</b>: its own hand at each decision, everything that has passed through
/// it, and the one discard offered to it each turn whether or not it took it. If the real rule
/// turns out to be that the piles may be read, this rung is merely <em>weaker</em> than the
/// rules allow; the other default would have had it seeing what the rules conceal. <b>Be wrong
/// in the direction that does not cheat.</b>
/// </para>
/// <para>
/// ⚠️ <b>The turned-up money cards are public, are plainly visible, and are deliberately not
/// counted.</b> Reading them would make which card designates money an input to a discard,
/// which RULES.md §4.4 forbids — and <c>MoneyCardsDoNotChangeWhatABotThrowsAway</c> is the test
/// that says so. The rule outranks the point of strength it would be worth.
/// </para>
/// <para>
/// 🔥 <b>The first rung in the ladder that remembers anything, so the round boundary is the
/// hazard.</b> A <see cref="CardId"/> names a card in a <em>round's</em> shoe, which is rebuilt
/// at every deal (P13.4), so memory carried across a deal would be memory of cards that no
/// longer exist. <see cref="Forget"/> is driven by <see cref="TurnContext.Round"/> and runs
/// before anything reads the memory, at every one of the four places this rung can be asked
/// something.
/// </para>
/// <para>
/// It is still deterministic: the same hand at the same point of the same round decides the
/// same way, and the memory is a function of the cards this seat was dealt and shown. Nothing
/// crosses a game, because an agent is built per seat per game (<see cref="BotRung.Create"/>).
/// </para>
/// </remarks>
public sealed class CountingBotAgent : IPlayerAgent, IRanksDiscards
{
    private const int SuitCount = 4;
    private const int RankCount = 13;

    /// <summary>The physical cards this seat has been shown, so that one is never counted twice.</summary>
    private readonly HashSet<CardId> _seen = [];

    /// <summary>How many copies of each value that comes to, indexed by <see cref="Slot"/>.</summary>
    private readonly int[] _copies = new int[RankCount * SuitCount];

    /// <summary>Which round the memory belongs to. Zero before the first turn of the match.</summary>
    private int _round;

    private readonly Func<Card, IReadOnlyList<Card>, long> _preference;
    private readonly ThreatScore.Supply _supply;

    /// <inheritdoc cref="CountingBotAgent" />
    public CountingBotAgent()
    {
        // Allocated once per seat per game rather than once per card judged: this is the one
        // rung whose supply estimate cannot be a static lambda, because it reads instance
        // state (BUILD-PLAN §3.7 item 4 — the work here is allocation-bound).
        _supply = Available;
        _preference = (card, hand) =>
        {
            var potential = CoverScore.Potential(card, hand);

            return potential == int.MaxValue
                ? long.MaxValue
                : ((long)potential << 8) + ThreatScore.Of(card, _supply);
        };
    }

    /// <summary>
    /// How many copies of one value this seat believes are still out there: two, less the ones
    /// it has been shown.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Never negative.</b> Two decks hold two copies of everything, so it cannot be — but
    /// the clamp is what stops a miscount turning a threat into a negative number and inverting
    /// a tie-break rather than merely getting it wrong.
    /// </remarks>
    public int Available(Rank rank, Suit suit) =>
        Math.Max(0, ThreatScore.CopiesInTheShoe - _copies[Slot(rank, suit)]);

    /// <summary>
    /// The physical cards this seat has been shown this round.
    /// </summary>
    /// <remarks>
    /// ✅ <b>Public because it is the packet's claim</b> (P20 acceptance 2): what a counting seat
    /// knows beyond its own hand has to be a subset of what a watcher at the table can see, and
    /// a claim nothing can read is a claim nothing can check. It is a copy, so reading it
    /// cannot change it.
    /// </remarks>
    public IReadOnlySet<CardId> Remembered => new HashSet<CardId>(_seen);

    /// <inheritdoc cref="GreedyBotAgent.ChooseAction"/>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Observe(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <inheritdoc cref="CautiousBotAgent.ChooseDiscard"/>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Observe(context);

        return CoverScore.Discard(context, _preference);
    }

    /// <inheritdoc cref="CautiousBotAgent.RankDiscards"/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Observe(context);

        return CoverScore.Ranking(context, _preference);
    }

    /// <inheritdoc cref="GreedyBotAgent.ClaimTurnedUpMoneyCard"/>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Observe(context);

        return context.TurnedUpMoneyCards.Count > 0
            && CoverScore.Improves(context.Hand, context.TurnedUpMoneyCards[^1]);
    }

    /// <inheritdoc cref="GreedyBotAgent.ObjectToClaim"/>
    public bool ObjectToClaim(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return true;
    }

    /// <summary>Always. The engine only asks when the hand genuinely wins (RULES.md §7.1).</summary>
    public bool Declare(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Observe(context);

        return true;
    }

    /// <summary>
    /// Takes in everything this seat is being shown right now — and nothing else.
    /// </summary>
    /// <remarks>
    /// <b>Every decision, not only the discard.</b> The engine builds a fresh context for each
    /// question in a turn (<c>RoundEngine.TakeTurn</c>), and the round reset has to have
    /// happened before whichever of them comes first — so this is called at all four rather
    /// than at the one that reads the memory.
    /// </remarks>
    private void Observe(TurnContext context)
    {
        Forget(context.Round);

        foreach (var card in context.Hand)
        {
            Remember(card);
        }

        if (context.AvailableDiscard is { } offered)
        {
            Remember(offered);
        }

        // context.TurnedUpMoneyCards is public and is not read here, on purpose. See the
        // remarks on this class: RULES.md §4.4.
    }

    /// <summary>Adds one physical card to the memory, if it is not there and is not a joker.</summary>
    /// <remarks>
    /// A joker has neither rank nor suit (<see cref="Card.IsJoker"/>), and the supply estimate
    /// is only ever asked about a ranked value, so counting one would have nowhere to go.
    /// </remarks>
    private void Remember(Card card)
    {
        if (!card.IsJoker && _seen.Add(card.Id))
        {
            _copies[Slot(card.Rank!.Value, card.Suit!.Value)]++;
        }
    }

    /// <summary>Drops the whole memory when the deal moves on.</summary>
    /// <remarks>
    /// ⚠️ <b>The round-boundary trap, and this rung is the first that can fall into it.</b> The
    /// shoe is rebuilt at every deal, so a <see cref="CardId"/> from the round before names a
    /// different physical card — a memory that survived a deal would not be stale, it would be
    /// wrong. <c>GreedyBotAgent</c>'s remark records the console agent falling into this once
    /// already.
    /// </remarks>
    private void Forget(int round)
    {
        if (round == _round)
        {
            return;
        }

        _round = round;
        _seen.Clear();
        Array.Clear(_copies);
    }

    private static int Slot(Rank rank, Suit suit) =>
        ((int)rank - (int)Rank.Two) * SuitCount + (int)suit;
}
