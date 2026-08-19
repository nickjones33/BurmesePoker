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
public sealed class SimpleBotAgent : IPlayerAgent
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
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var hand = context.Hand;
        Card? best = null;
        var bestScore = int.MinValue;
        var judged = new List<Card>(hand.Count);

        foreach (var card in hand)
        {
            // The same dedup the greedy bot does, so that the two differ in the tie-break and
            // in nothing else — including what they cost to run.
            if (judged.Exists(seen => seen.SameValueAs(card)))
            {
                continue;
            }

            judged.Add(card);

            var score = CoverScore.Covered(CoverScore.Without(hand, card));

            if (best is null || score > bestScore)
            {
                best = card;
                bestScore = score;
            }
        }

        return best ?? throw new InvalidOperationException("Asked to discard from an empty hand.");
    }

    /// <inheritdoc cref="GreedyBotAgent.ClaimTurnedUpMoneyCard"/>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TurnedUpMoneyCards.Count > 0
            && CoverScore.Improves(context.Hand, context.TurnedUpMoneyCards[^1]);
    }

    /// <summary>Go out the moment it can: there is never a reason to hold a winning hand.</summary>
    public bool Declare(TurnContext context) => true;
}
