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
/// before anything reads the memory, at four of the five places this rung can be asked
/// something — <see cref="ObjectToClaim"/> deliberately does not <c>Observe</c>, so a future
/// counting rung that reads the memory there would meet a stale one.
/// </para>
/// <para>
/// It is still deterministic: the same hand at the same point of the same round decides the
/// same way, and the memory is a function of the cards this seat was dealt and shown. Nothing
/// crosses a game, because an agent is built per seat per game (<see cref="BotRung.Create"/>).
/// </para>
/// </remarks>
public sealed class CountingBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <summary>
    /// The memory itself, which P31 lifted out of this class whole.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Nothing about it changed in the move</b> — <see cref="ShoeMemory"/> is this rung's
    /// old fields and its old <c>Observe</c>/<c>Forget</c>/<c>Available</c>, verbatim, so a
    /// difference in this rung's results across P31 would be a defect and not a redesign.
    /// </remarks>
    private readonly ShoeMemory _memory = new();

    private readonly Func<Card, IReadOnlyList<Card>, long> _preference;

    /// <inheritdoc cref="CountingBotAgent" />
    public CountingBotAgent()
    {
        // Allocated once per seat per game rather than once per card judged: this is the one
        // rung whose supply estimate cannot be a static lambda, because it reads instance
        // state (BUILD-PLAN §3.7 item 4 — the work here is allocation-bound).
        var supply = _memory.Supply;

        _preference = (card, hand) =>
        {
            var potential = CoverScore.Potential(card, hand);

            return potential == int.MaxValue
                ? long.MaxValue
                : ((long)potential << 8) + ThreatScore.Of(card, supply);
        };
    }

    /// <inheritdoc cref="ShoeMemory.Available"/>
    public int Available(Rank rank, Suit suit) => _memory.Available(rank, suit);

    /// <inheritdoc cref="ShoeMemory.Remembered"/>
    public IReadOnlySet<CardId> Remembered => _memory.Remembered;

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

    /// <inheritdoc/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);
        Observe(context);

        return CoverScore.Ranking(context.Hand, candidates, _preference);
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
    /// <b>Every on-turn decision, not only the discard.</b> The engine builds a fresh context
    /// for each question in a turn (<c>RoundEngine.TakeTurn</c>), and the round reset has to
    /// have happened before whichever of them comes first — so this is called at all four
    /// on-turn questions rather than at the one that reads the memory.
    /// <see cref="ObjectToClaim"/>, the fifth, deliberately does not call it: a future counting
    /// rung reading the memory there would meet a stale one.
    /// </remarks>
    private void Observe(TurnContext context) => _memory.Observe(context);
}
