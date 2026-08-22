using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// The same seat as <see cref="GreedyBotAgent"/> with the tie-break taken out: it throws the
/// first card, in the order it happens to be holding them, that costs it no melded card.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a control, not an attempt at a better player.</b> P10 claimed the tie-break —
/// keep cards with partners, keep jokers over everything — is what actually makes progress,
/// because early in a round almost every discard scores alike and the cover count alone
/// cannot separate them. That claim is measurable exactly once there are two strategies
/// identical but for it, which is what this is (BUILD-PLAN P12).
/// </para>
/// <para>
/// It shares the take, claim and declare rules with <see cref="GreedyBotAgent"/> — all three
/// turn on a strict improvement in the cover count, where there is no tie to break — so a
/// difference in results is attributable to the discard and to nothing else.
/// </para>
/// <para>
/// <b>It is not guaranteed to terminate.</b> Its score can never fall, for the same reason
/// the greedy bot's cannot, but nothing pushes it off a plateau: a table of these can pass a
/// hand round for ever, which is why a simulation harness bounds a round itself
/// (BUILD-PLAN P12).
/// </para>
/// </remarks>
public sealed class SimpleBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <inheritdoc cref="GreedyBotAgent.ChooseAction"/>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary>Throw whichever card costs the fewest melded cards, first one wins.</summary>
    /// <remarks>
    /// The same loop <see cref="GreedyBotAgent"/> throws through, handed a tie-break that
    /// breaks nothing — which is the difference between the two rungs stated as code
    /// (BUILD-PLAN P15).
    /// </remarks>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Discard(context, CoverScore.NoPreference);
    }

    /// <summary>The same ordering the discard is the head of (BUILD-PLAN P19).</summary>
    /// <remarks>
    /// <b>One call, not two.</b> <see cref="ChooseDiscard"/> is defined as the first of these, so a level built on
    /// this rung slips to a card this rung genuinely considered rather than to one somebody
    /// thought it might have.
    /// </remarks>
    public IReadOnlyList<Card> RankDiscards(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Ranking(context, CoverScore.NoPreference);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Card> RankDiscards(TurnContext context, IReadOnlyList<Card> candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);

        return CoverScore.Ranking(context.Hand, candidates, CoverScore.NoPreference);
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

    /// <summary>Go out the moment it can: there is never a reason to hold a winning hand.</summary>
    public bool Declare(TurnContext context) => true;
}
