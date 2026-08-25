using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="OutsBotAgent"/> that prices every pickup against the deck: a card off the pile is
/// taken only when the hand it leaves is worth more than the blind draw it forfeits — and that
/// cuts both ways, so a card that melds nothing <em>today</em> is still taken when it opens more
/// doors than the deck was expected to (BUILD-PLAN P45).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The question is the one <c>docs/STRATEGY.md</c> §8 explicitly left open.</b>
/// <c>warden</c> paid a draw for every lock and nothing in its rule priced the draw; the
/// autopsy's last line was that nothing yet prices a draw <em>in cards</em>, and that
/// <see cref="LiveOuts"/> is the obvious currency. This rung is that price: a blind draw is
/// expected to deliver its <b>live out-cards over the cards unseen</b> — each value the hand is
/// waiting on, weighed by its loose copies (<see cref="LiveOuts.CardCount"/>), over everything
/// this seat cannot see. <c>outs</c> takes any discard that helps at all and only a discard
/// that helps at all; this rung asks whether either half of that rule mis-prices the take.
/// </para>
/// <para>
/// ⚠️ <b>The exchange model is stated rather than tuned, and it is one sentence</b>
/// (<see cref="ProspectorBotAgent.WinShareOfACoveredCard"/>'s precedent): <b>a hand is worth
/// its melded cards plus exactly one blind draw's expectation, and a draw is priced as leaving
/// the outs profile where it was.</b> Take the offered card when the thirteen it leaves beats
/// that — <c>gain·unseen + outsAfter &gt; 2·outsNow</c>, the same inequality with the
/// probabilities multiplied out, so the whole comparison is integer arithmetic over public
/// facts. A longer horizon would compound the enrichment and take <em>more</em> often, so the
/// one-draw horizon errs towards <c>outs</c>' own behaviour, which is the conservative
/// direction for the rung being measured against it.
/// </para>
/// <para>
/// <b>One change, made at both places it arises</b> (<see cref="ProspectorBotAgent"/>'s rule):
/// taking the previous player's discard and claiming the turned-up money card are the same
/// purchase — a known card bought with this turn's blind draw — so both go through the same
/// comparison. The discard, the ranking and the declaration are <see cref="OutsBotAgent"/>'s
/// card for card, so a difference in results attributes to the take (P15).
/// </para>
/// <para>
/// ⚠️ <b>What the two directions of the rule actually do.</b> Refusing an improving card
/// requires the forfeited draw to outprice a certain melded card — <c>2·outsNow</c> against a
/// whole unseen pool of ~90 — which real hands essentially never reach, so the improving take
/// is expected to stay <c>outs</c>' in practice. The live direction is the <b>enrichment
/// take</b>: a card that raises no meld today but more than doubles the hand's live out-cards
/// is worth more than the draw it costs. Whether that fires often enough to matter is the
/// packet's question, and the take rate at the crossed table is its mechanism variable.
/// </para>
/// <para>
/// ⚠️ <b>Three approximations in the lookahead, all stated.</b> (1) The thirteen a take would
/// leave is estimated by the cover-and-partnership keys alone, filtered by the same §5.1
/// legality the real choice will meet — the ban state cannot change between the take and the
/// discard, they are one turn — while the real discard, made a moment later, adds the outs
/// refinement; the estimate is what keeps the price affordable (P21's budget), and it errs by
/// undervaluing the take, which is the conservative direction. (2) An enrichment take requires
/// the offered card itself to be meldable with the hand
/// (<see cref="LiveOuts.CouldJoinAMeld(Card,IReadOnlyList{Card})"/>) — a rule as well as a
/// prune, because without it the model would sometimes take an inert card purely to shed a
/// blocked duplicate, a benefit the blind draw's own discard collects just as well. (3) The
/// unseen pool is <see cref="MoneyOdds"/>' — the shoe less this seat's own view — and holds
/// opponents' hands and piles as well as the draw pile, which overstates what a draw can reach;
/// the density argument is that file's, and the error is towards drawing blind, the behaviour
/// being measured rather than assumed.
/// </para>
/// </remarks>
public sealed class AnglerBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <summary>The whole shoe, which is common knowledge (RULES.md §2).</summary>
    private static readonly int CardsInTheShoe = DeckBuilder.BuildTwoDecks().Count;

    /// <inheritdoc cref="OutsBotAgent"/>
    private readonly OutsCache _cache = new();

    /// <summary>
    /// Take the offered card only when the hand it leaves beats the blind draw it forfeits.
    /// </summary>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return WorthForfeitingTheDraw(context, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary>Greedy's discard, with the tie decided by what the hand would still be waiting for.</summary>
    /// <remarks>⚠️ <b><c>outs</c>' exactly</b> — the change is the take, and only the take (P15).</remarks>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Discard(context, Preference, Outs);
    }

    /// <inheritdoc cref="OutsBotAgent.RankDiscards"/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Ranking(context, Preference, Outs);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);

        return CoverScore.Ranking(context.Hand, candidates, Preference, Outs);
    }

    /// <summary>
    /// The same purchase at the other place it arises: a claimed card is a known card bought
    /// with this turn's blind draw (RULES.md §4.5), so it pays the same toll.
    /// </summary>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnedUpMoneyCards.Count > 0
            && WorthForfeitingTheDraw(context, context.TurnedUpMoneyCards[^1]);
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
    /// Whether a known card is worth this turn's blind draw:
    /// <c>gain·unseen + outsAfter &gt; 2·outsNow</c>, strict, so an even choice goes to the
    /// deck — greedy's own convention, and the deck's cards are the ones that pay (§4.4).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The expensive half is paid only when it could change the answer.</b> When
    /// <c>gain·unseen</c> alone clears <c>2·outsNow</c> the kept thirteen cannot matter —
    /// <c>outsAfter</c> is never negative — so every ordinary improving take costs one cover
    /// call and one out-count over a hand whose probes the seat's own ranking has mostly cached.
    /// The lookahead ranking of the fourteen is reached only where the hand's outs are thin or
    /// the card melds nothing, which is exactly where the question is live.
    /// </remarks>
    private bool WorthForfeitingTheDraw(TurnContext context, Card offered)
    {
        var hand = context.Hand;
        var covered = CoverScore.Covered(hand);
        var fourteen = new List<Card>(hand.Count + 1);

        fourteen.AddRange(hand);
        fourteen.Add(offered);

        var gain = CoverScore.Covered(fourteen) - covered;
        var unseen = CardsUnseen(context);
        var outsNow = LiveOuts.CardCount(hand, covered, _cache);

        if (gain * unseen > 2 * outsNow)
        {
            return true;
        }

        // An enrichment take is a take of a card that could itself sit in a meld with what is
        // held — the gate is one scan, and it is a rule rather than only a prune: without it the
        // model would sometimes take an inert card purely to shed a blocked duplicate, a benefit
        // the blind draw's discard collects just as well.
        if (gain == 0 && !LiveOuts.CouldJoinAMeld(offered, hand))
        {
            return false;
        }

        // The thirteen a take would leave, estimated by the cover-and-partnership keys alone —
        // the refinement's probes are the cost this lookahead exists to weigh, not to pay —
        // under the same §5.1 filter the real choice will meet (the ban state is this turn's).
        var legal = context.ClosedToYou.LegalDiscards(fourteen, context.Rules);
        var best = CoverScore.Scored(fourteen, legal, Preference)[0];
        var kept = CoverScore.Without(fourteen, best.Card);
        var outsAfter = LiveOuts.CardCount(kept, best.Covered, _cache);

        return (gain * unseen) + outsAfter > 2 * outsNow;
    }

    /// <summary>
    /// What this seat cannot see: the shoe less its own hand, the turned-up cards still on the
    /// table, and the discard in front of it — <see cref="MoneyOdds"/>' view of the same pool.
    /// </summary>
    private static int CardsUnseen(TurnContext context) =>
        CardsInTheShoe
        - context.Hand.Count
        - context.TurnedUpMoneyCards.Count
        - (context.AvailableDiscard is null ? 0 : 1);

    /// <summary>More outs is better, and the ranking takes the lowest key first.</summary>
    private long Outs(IReadOnlyList<Card> kept, int covered) => -LiveOuts.Count(kept, covered, _cache);

    /// <summary>Greedy's key, unchanged, deciding everything the count of outs leaves tied.</summary>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference =
        static (card, hand) => CoverScore.Potential(card, hand);
}
