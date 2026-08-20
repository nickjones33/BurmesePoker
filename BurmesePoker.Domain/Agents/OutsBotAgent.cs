using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// <see cref="GreedyBotAgent"/> that looks one card ahead: where two discards leave the hand
/// equally melded, it keeps the thirteen that <b>more of the shoe would improve</b>
/// (BUILD-PLAN P21).
/// </summary>
/// <remarks>
/// <para>
/// <b>Only the last resort changes</b>, exactly as <see cref="CautiousBotAgent"/>'s does.
/// Greedy compares the cover count first, and where it ties it falls back on how many partners
/// the thrown card has. This rung puts one question in between: of the thirteen that would be
/// left, how many of the values still out there would raise the count if the next draw brought
/// one? Throw whichever leaves the most (<see cref="LiveOuts"/>). Where <em>that</em> ties, it
/// is greedy again, card for card.
/// </para>
/// <para>
/// 🔥 <b>It is the first rung in the programme that is not greedy under another name.</b> P15
/// proved that every pairwise-additive tie-break is: partnership is symmetric, so "throw the
/// fewest partners" and "keep the best-connected twelve" are one rule, and
/// <see cref="CautiousBotAgent"/> and <see cref="CountingBotAgent"/> both live in the residue
/// that leaves. A count of outs is a question about the whole arrangement of the thirteen — two
/// cards with identical partners can leave different numbers of outs — so this is the first
/// rung with somewhere new to stand, and P21 is the packet that measures whether standing there
/// is worth anything.
/// </para>
/// <para>
/// ⚠️ <b>What it does not know is deliberate.</b> It remembers nothing, so a card it watched go
/// by is still live to it (<see cref="ThreatScore.NotInThisHand"/>); money is absent from every
/// decision, as it is from every rung; and it looks exactly one card ahead and never two. Each
/// of those would be a second change, and a rung two changes from the one below it measures
/// nothing (P15).
/// </para>
/// <para>
/// ⚠️ <b>The cost is the reason this is a packet and not a tie-break.</b> A naive reading asks a
/// partial cover of every value against every candidate — roughly 100× a greedy decision. What
/// makes it affordable is entirely <em>around</em> the evaluator: the refinement is asked only
/// of candidates already tied at the top (<see cref="CoverScore.Refinement"/>), values that
/// could not enter a meld at all are never searched, and <see cref="OutsCache"/> remembers an
/// answer by the values of the fourteen cards it was asked about. Nothing inside
/// <see cref="Melds.HandEvaluator"/> or <see cref="Melds.PartialCover"/> changed, which is
/// BUILD-PLAN §3.4 as a rule of engagement rather than as advice.
/// </para>
/// </remarks>
public sealed class OutsBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <summary>
    /// Answers already bought, kept for the life of the seat.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>It survives a deal, and that is safe for exactly one reason: it names no
    /// <see cref="CardId"/>.</b> A <c>CardId</c> means a card in a <em>round's</em> shoe, which
    /// is rebuilt every deal (P13.4), and that is what made <see cref="CountingBotAgent"/> have
    /// to forget. This remembers "thirteen of these values cover eleven", which is a fact about
    /// the rules and not about the round.
    /// </remarks>
    private readonly OutsCache _cache = new();

    /// <inheritdoc cref="GreedyBotAgent.ChooseAction"/>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary>Greedy's discard, with the tie decided by what the hand would still be waiting for.</summary>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Discard(context.Hand, Preference, Outs);
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

        return CoverScore.Ranking(context.Hand, Preference, Outs);
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

    /// <summary>More outs is better, and the ranking takes the lowest key first.</summary>
    private long Outs(IReadOnlyList<Card> kept, int covered) => -LiveOuts.Count(kept, covered, _cache);

    /// <summary>Greedy's key, unchanged, deciding everything the count of outs leaves tied.</summary>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference =
        static (card, hand) => CoverScore.Potential(card, hand);
}
