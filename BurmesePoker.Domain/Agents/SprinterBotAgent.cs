using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="OutsBotAgent"/> that runs for the line once it is in sight: within one card of
/// covering it stops keeping the hand the most of the pack would help and keeps the one the most
/// of the pack would <b>win</b>, trading expected improvement for the chance to declare first
/// (BUILD-PLAN P46).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The idea is the fact behind <c>docs/STRATEGY.md</c> §6's √2: exactly one seat
/// declares.</b> A round is a race to go out, not a contest of hand quality, and the two objectives
/// come apart at the end — <c>outs</c> keeps the thirteen the widest slice of the deck would
/// <em>improve</em>, which near the line is not the same as the thirteen the widest slice would
/// <em>complete</em>. A hand two draws from a strong shape can be worth less, in the only currency
/// that settles a round, than a hand one draw from a weak one. This rung prefers the shorter fuse.
/// </para>
/// <para>
/// ⚠️ <b>One change, and it is the discard's last-resort key — <c>purist</c>'s idiom.</b> Taking,
/// claiming, objecting and declaring are <see cref="OutsBotAgent"/>'s card for card, so a
/// difference in results attributes to the endgame discard and to nothing else (P15). The key is
/// lexicographic: <b>winning draws first, then <c>outs</c>' own live-out count</b>
/// (<see cref="LiveOuts.WinningDraws"/> over <see cref="LiveOuts.Count"/>), packed into one
/// <see cref="long"/> so the sort reads it as it reads every other tie-break.
/// </para>
/// <para>
/// 🔥 <b>The trigger is not a threshold anybody tuned — it is whether a winning draw exists at
/// all.</b> A value counts as a winning draw only when it would let thirteen of the fourteen it
/// leaves meld (<see cref="LiveOuts.WinningDraws"/>, bar = the hand's own size), which cannot
/// happen unless the hand is already within one card of covering. So off the endgame every
/// candidate scores zero winning draws and the key collapses to <c>-outs</c> — this rung
/// <em>is</em> <c>outs</c>, card for card, until the fuse is short (P45's enrichment-take lesson:
/// price the move, never tune when it fires). ⚠️ <b>That the regime is reached at all is the
/// mechanism variable</b> P46 publishes beside the margin (<see cref="Endgame"/>,
/// <c>ladder.race-reach.*</c>): a hand rarely gets within one card of covering on its own discard
/// turn before somebody declares, and a flat reach rate would be the finding rather than a
/// failure.
/// </para>
/// <para>
/// ⚠️ <b>It buys a worse hand in expectation, on purpose.</b> Among discards that leave the same
/// number of cards melded, the one with the most winning draws often has <em>fewer</em> improving
/// draws in total — it is more committed to one near-meld and less able to grow any other way. That
/// is the trade the packet measures: a faster chance to win against a broader chance to improve,
/// with cover count itself never sacrificed (the winning key sits below <c>outs</c>' cover key, as
/// <c>outs</c>' out-count sits below greedy's).
/// </para>
/// </remarks>
public sealed class SprinterBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <summary>Room above the out-count for the winning-draw key to dominate it (BUILD-PLAN P46).</summary>
    /// <remarks>
    /// The out-count is a tally of the 53 distinct values, so it never reaches 53; a scale of
    /// 1,000 keeps the two keys lexicographic — any winning draw outranks any number of ordinary
    /// outs — inside one <see cref="long"/>, which is what lets the existing sort break the tie.
    /// </remarks>
    private const long WinningWeight = 1_000;

    /// <inheritdoc cref="OutsBotAgent"/>
    private readonly OutsCache _cache = new();

    /// <inheritdoc cref="OutsBotAgent.ChooseAction"/>
    /// <remarks>⚠️ <b><c>outs</c>' exactly</b> — the change is the endgame discard, and only that (P15).</remarks>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary>Greedy's discard, with the tie decided by the fuse near the line and by outs elsewhere.</summary>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Discard(context, Preference, Race);
    }

    /// <inheritdoc cref="OutsBotAgent.RankDiscards(TurnContext)"/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Ranking(context, Preference, Race);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);

        return CoverScore.Ranking(context.Hand, candidates, Preference, Race);
    }

    /// <inheritdoc cref="OutsBotAgent.ClaimTurnedUpMoneyCard"/>
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

    /// <summary>
    /// Winning draws first, then <c>outs</c>' live-out count — one lexicographic key, and off the
    /// endgame exactly <c>-outs</c>, because no candidate has a winning draw until the hand is
    /// within one card of covering.
    /// </summary>
    private long Race(IReadOnlyList<Card> kept, int covered)
    {
        var winning = LiveOuts.WinningDraws(kept, covered, _cache);
        var outs = LiveOuts.Count(kept, covered, _cache);

        return -((winning * WinningWeight) + outs);
    }

    /// <summary>Greedy's key, unchanged, deciding everything the fuse leaves tied.</summary>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference =
        static (card, hand) => CoverScore.Potential(card, hand);
}
