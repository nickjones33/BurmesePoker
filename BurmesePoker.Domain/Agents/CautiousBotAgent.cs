using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="GreedyBotAgent"/> with one thing added: when two cards cost the same and are
/// equally worth keeping, it throws the one an opponent is least likely to be able to use
/// (BUILD-PLAN P15).
/// </summary>
/// <remarks>
/// <para>
/// <b>Only the last resort changes.</b> Greedy compares the cover count first and the card's
/// own partners second, and where both tie it throws whichever card it happened to reach
/// first. This rung fills that arbitrary place with a reason — so it is the same player,
/// deciding the same way, right up to the point where greedy stops deciding at all.
/// </para>
/// <para>
/// <b>"The player it feeds" is an unseen hand, and can be nothing else.</b> Play is fully
/// concealed (RULES.md §6.3) and a <c>TurnContext</c> is that rule as a type: there is no way
/// from a seat to the next seat's cards. So the question is answered against the only opponent
/// the rules make knowable — one holding cards drawn from what this hand cannot see — and the
/// measure is how many still-unseen pairs would put the thrown card into a meld.
/// </para>
/// <para>
/// ⚠️ <b>A prediction this rung is built to test, and it is not a flattering one.</b> Denial
/// and self-interest point the same way here: the partners this hand holds are exactly the
/// partners an opponent cannot hold, so "least use to me" and "least use to them" are close to
/// the same ordering, and <see cref="CoverScore.Potential"/> has already spent that
/// information. What is left for this measure is what a hand cannot influence — how many melds
/// a rank could join at all, and how much of the supply is blocked in more than one place at
/// once. <b>That is a small residue, so a small effect is the honest expectation</b>, and
/// P15's acceptance is that the rungs are <em>measured</em> rather than asserted.
/// </para>
/// <para>
/// It is also P16's intervention: a seat that throws what is least useful to whoever it feeds
/// is the directional test of the upstream hypothesis. The caveat above is a prediction about
/// that experiment too — a weak denier can only produce a weak intervention.
/// </para>
/// <para>
/// ⚠️ <b>The measure itself moved to <see cref="ThreatScore"/> in P20 and did not change.</b>
/// This rung still estimates what is left in the shoe from its own hand alone
/// (<see cref="ThreatScore.NotInThisHand"/>); <see cref="CountingBotAgent"/> is the same rung
/// estimating it from everything it has been shown, and that one substitution is all that
/// separates them.
/// </para>
/// </remarks>
public sealed class CautiousBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <inheritdoc cref="GreedyBotAgent.ChooseAction"/>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary>Greedy's discard, with the arbitrary part decided by what it gives away.</summary>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Discard(context, Preference);
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

        return CoverScore.Ranking(context, Preference);
    }

    /// <inheritdoc cref="GreedyBotAgent.ClaimTurnedUpMoneyCard"/>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnedUpMoneyCards.Count > 0
            && CoverScore.Improves(context.Hand, context.TurnedUpMoneyCards[^1]);
    }

    /// <summary>Always. The engine only asks when the hand genuinely wins (RULES.md §7.1).</summary>
    public bool Declare(TurnContext context) => true;

    /// <summary>
    /// Greedy's key with the threat packed underneath it: the partners this hand holds decide
    /// first, and what the card is worth to somebody else decides only where they tie.
    /// </summary>
    /// <remarks>
    /// A joker keeps its <see cref="int.MaxValue"/>, so it is still the last card in the hand
    /// this rung will ever part with. Everything else fits: the threat is at most 24, which is
    /// why eight bits are enough to carry it.
    /// </remarks>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference = static (card, hand) =>
    {
        var potential = CoverScore.Potential(card, hand);

        return potential == int.MaxValue
            ? long.MaxValue
            : ((long)potential << 8) + ThreatScore.Of(card, ThreatScore.NotInThisHand(hand));
    };
}
