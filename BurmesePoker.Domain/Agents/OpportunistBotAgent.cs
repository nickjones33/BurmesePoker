using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="OutsBotAgent"/> that keeps the locks its ordinary takes arm: it never spends a
/// turn to deny anybody anything, but a rank its take has closed against the seat above stays
/// closed (BUILD-PLAN P43).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The question it exists to split, stated exactly.</b> P31 showed that §5.1's locks bite
/// hard — the ban removes a held card from 30.5% of discards in a crossed field and changes the
/// answer on 9.4% of every turn — and that <c>warden</c> still lost by more than any rung has
/// lost before, because it pays for locks with draws and nothing in its rule prices a draw.
/// That left two readings nothing could tell apart: <em>denial is worthless</em>, or
/// <em>`warden`'s pricing was wrong</em>. This rung is the instrument: every take is one
/// <c>outs</c> would have made anyway, so the lock arrives at <b>zero price</b> and whatever
/// the margin says is the value of the hold alone.
/// </para>
/// <para>
/// ⚠️ <b>It is one decision from each of its neighbours, which is what makes it measurable</b>
/// (P15). From <c>outs</c> it differs only in the hold — the take, the ranking, the refinement,
/// the claim and the declaration are <c>outs</c>' card for card. From <c>warden</c> it differs
/// only in never buying a lock — the hold is literally shared code (<see cref="HeldLocks"/>).
/// The 2×2 corner the branch was missing: take-for-denial no, hold yes.
/// </para>
/// <para>
/// ⚠️ <b>The hold is free to arm and not free to keep</b>, which is what the measurement is
/// for. An improving take costs nothing extra at the moment it is made, but every rank held is
/// a discard the ranking may not choose later — the seat pays in flexibility, not in draws.
/// The hold keeps §5.1's own two escapes (the declaring discard, and the floor when nothing
/// else is legal), so it can never cost the round outright or deadlock a turn.
/// </para>
/// </remarks>
public sealed class OpportunistBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <inheritdoc cref="OutsBotAgent"/>
    private readonly OutsCache _cache = new();

    /// <summary>
    /// <c>outs</c>' take, exactly — a card is taken because it improves the hand, never for the
    /// lock. The lock is a side effect the rung then declines to give back.
    /// </summary>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary><c>outs</c>' discard, chosen from the cards it is still willing to throw.</summary>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Ranking(context.Hand, HeldLocks.Candidates(context), Preference, Outs) is [var best, ..]
            ? best
            : throw new InvalidOperationException("Asked to discard from an empty hand.");
    }

    /// <inheritdoc cref="OutsBotAgent.RankDiscards"/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Ranking(context.Hand, HeldLocks.Candidates(context), Preference, Outs);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// ⚠️ <b>The restraint is not the ban and stays in force here</b>, so the counterfactual this
    /// answers is <em>what §5.1 cost me</em> and not <em>what my own holds cost me</em> — the
    /// same reading <see cref="WardenBotAgent"/> gives it (BUILD-PLAN P31 item 3).
    /// </remarks>
    public IReadOnlyList<Card> RankDiscards(TurnContext context, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);

        return CoverScore.Ranking(context.Hand, HeldLocks.Willing(context, candidates), Preference, Outs);
    }

    /// <inheritdoc cref="GreedyBotAgent.ClaimTurnedUpMoneyCard"/>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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
    public bool Declare(TurnContext context) => true;

    /// <summary>More outs is better, and the ranking takes the lowest key first.</summary>
    private long Outs(IReadOnlyList<Card> kept, int covered) => -LiveOuts.Count(kept, covered, _cache);

    /// <summary>Greedy's key, unchanged, deciding everything the count of outs leaves tied.</summary>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference =
        static (card, hand) => CoverScore.Potential(card, hand);
}
