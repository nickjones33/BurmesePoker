using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// A seat played by the computer: take a card only when it melds more of the hand, throw away
/// whatever costs the least, and go out the moment it can.
/// </summary>
/// <remarks>
/// <para>
/// <b>A strategy is just another <see cref="IPlayerAgent"/></b> (BUILD-PLAN P10). There is no
/// strategy abstraction on top of one that already exists, so this class is swappable for a
/// human seat, a script, or the next strategy along, and the engine cannot tell the difference.
/// It lives in the domain rather than the console because it is rules reasoning with no I/O —
/// which is also what makes it the one kind of player that can be unit-tested.
/// </para>
/// <para>
/// <b>Every decision is the same question:</b> of the thirteen cards I would be left holding,
/// how many meld? That is <see cref="PartialCover.CoveredCount"/>, and it is the whole of the
/// strategy. The score can never fall — throwing back the card just taken restores the hand
/// exactly — so a bot's hand improves monotonically towards the thirteen-of-thirteen that ends
/// the round (RULES.md §7.1), which is what makes a table of bots terminate at all.
/// </para>
/// <para>
/// <b>Money cards are deliberately absent from all of it.</b> Ownership is permanent and never
/// transfers (RULES.md §4.4): a money card the deck gave you pays you whether you are still
/// holding it or threw it away four turns ago. So holding one gains nothing, hoarding one is
/// simply playing a worse hand for no return, and the only place money touches a decision at
/// all is the tie-break below — a blind draw confers ownership, so an even choice goes to the
/// deck rather than to the discard pile.
/// </para>
/// <para>
/// <b>Nothing here is random and nothing here is remembered.</b> The same hand facing the same
/// table always decides the same way, which is what a reproducible simulation needs
/// (BUILD-PLAN §3.7), and holding no per-turn state avoids the round-boundary trap the console
/// agent fell into.
/// </para>
/// </remarks>
public sealed class GreedyBotAgent : IPlayerAgent, IRanksDiscards
{
    /// <summary>
    /// Take the discard only if it melds more of the hand than what is already held.
    /// </summary>
    /// <remarks>
    /// The comparison is strict, and that is the one place the money layer enters a decision:
    /// a blind draw comes from the deck and so confers ownership (RULES.md §4.4), while a card
    /// taken from a discard pile never does. An even choice therefore goes to the deck.
    /// </remarks>
    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discard = context.AvailableDiscard
            ?? throw new InvalidOperationException("Asked how to take a card with no discard available.");

        return CoverScore.Improves(context.Hand, discard) ? TurnAction.TakeDiscard : TurnAction.DrawFromDeck;
    }

    /// <summary>
    /// Throw whichever card costs the fewest melded cards, and break ties towards the card
    /// with the least chance of ever joining one.
    /// </summary>
    /// <remarks>
    /// A card any meld in the best arrangement needs can never be the answer: throwing it
    /// breaks a meld of at least three and so scores worse than throwing loose deadwood. The
    /// tie-break is what makes progress, because early on most discards score the same — it
    /// prefers to keep cards with partners in the hand (another suit of the same rank, or a
    /// neighbour in the same suit), and keeps jokers over everything, since a joker fits
    /// anywhere.
    /// </remarks>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CoverScore.Discard(context.Hand, Preference);
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

        return CoverScore.Ranking(context.Hand, Preference);
    }

    /// <summary>
    /// The tie-break itself, kept as a field so that the ladder's rungs are one function
    /// apart and nothing else (BUILD-PLAN P15).
    /// </summary>
    private static readonly Func<Card, IReadOnlyList<Card>, long> Preference =
        static (card, hand) => CoverScore.Potential(card, hand);

    /// <summary>
    /// Take the turned-up money card only if it melds more of the hand.
    /// </summary>
    /// <remarks>
    /// It is a pure hand decision and nothing else. The table gives the card, not the deck, so
    /// it is held but never owned and <b>pays nobody</b> (RULES.md §4.5) — there is no money
    /// reason to want it. It also costs the turn's draw, so a card that changes nothing is
    /// strictly worse than drawing one that might.
    /// </remarks>
    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The two turned-up cards lie bottom-first, so the last is the claimable one off the
        // top of the deck (RULES.md §3 step 4, §4.5).
        return context.TurnedUpMoneyCards.Count > 0
            && CoverScore.Improves(context.Hand, context.TurnedUpMoneyCards[^1]);
    }

    /// <summary>Always. The engine only asks when the hand genuinely wins (RULES.md §7.1).</summary>
    public bool Declare(TurnContext context) => true;
}
