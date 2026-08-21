using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// What a blind draw is worth in <em>ownership</em> — the one thing the deck gives that a
/// discard pile never can (RULES.md §4.4, BUILD-PLAN P22).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is the only money arithmetic any rung does, and it is deliberately nowhere near a
/// discard.</b> Ownership is permanent and never transfers, so holding a money card is worth
/// nothing and throwing one away costs nothing — which is exactly why every rung's discard is
/// money-blind and <c>MoneyCardsDoNotChangeWhatABotThrowsAway</c> is a test. What the rule does
/// <em>not</em> settle is where a card comes <b>from</b>: a card the deck hands you pays you for
/// the rest of the round, and the identical card off the pile beside you pays you nothing. So a
/// take is a small purchase made with a lottery ticket, and this is the price of the ticket.
/// </para>
/// <para>
/// <b>The estimate uses only public information and no memory.</b> Two decks are common
/// knowledge, the designation is public (RULES.md §4.1) and the stakes are fixed at the start of
/// the game (§4.3); what is subtracted is what this seat can see — its own thirteen, the cards
/// still turned up on the table, and the discard it is being offered. Nothing here reads a
/// discard pile, so RULES.md §9 #15 stays open and undecided in code, exactly as P20 left it.
/// </para>
/// <para>
/// ⚠️ <b>Two approximations, both stated rather than hidden.</b> (1) The unseen pool holds
/// opponents' hands and the discard piles as well as the draw pile, and only the draw pile is
/// reachable — but a money card is no likelier to be in one than the other, so the <em>density</em>
/// is right even though the pool is too big. (2) After a reshuffle (RULES.md §5) a drawn card may
/// already belong to somebody, and first acquisition wins
/// (<see cref="CardOwnership.TryRecordFromDeck"/>), so late in a long round this overstates.
/// Reshuffles are rare (RULES.md §5) and the overstatement is in the direction of drawing blind,
/// which is the behaviour being measured rather than assumed.
/// </para>
/// <para>
/// ⚠️ <b>A third, added by P26: the ×5 is not priced.</b> A draw that completes both partners of
/// a 7♦/A♠ turn-up is worth ten money-card values rather than six (RULES.md §4.1), and this sums
/// <see cref="MoneyCardRegistry.Multiplier(Card)"/> over the shoe, which is the value-only
/// answer. Pricing it would need the seat's own ownership records, which no rung reads; the
/// turn-up is that pair about one round in 1,400, and the understatement is against drawing
/// blind, which is the conservative direction for the one rung that reads this at all.
/// </para>
/// </remarks>
internal static class MoneyOdds
{
    /// <summary>
    /// The shoe as a list of <em>values</em>, which is common knowledge and not table state.
    /// </summary>
    /// <remarks>
    /// Built once. The ids are the shoe's own (<see cref="DeckBuilder.BuildTwoDecks"/>) and are
    /// never compared with anything: every question asked of this list is a question about
    /// value, which is the distinction BUILD-PLAN §3.1 exists to keep visible.
    /// </remarks>
    private static readonly IReadOnlyList<Card> Shoe = DeckBuilder.BuildTwoDecks();

    /// <summary>What one blind draw is worth to the seat being asked, in money.</summary>
    internal static double PerBlindDraw(TurnContext context)
    {
        var seen = new List<Card>(context.Hand.Count + context.TurnedUpMoneyCards.Count + 1);

        seen.AddRange(context.Hand);
        seen.AddRange(context.TurnedUpMoneyCards);

        if (context.AvailableDiscard is { } offered)
        {
            seen.Add(offered);
        }

        return PerBlindDraw(context.MoneyCards, seen, context.Stakes, context.Players.Count);
    }

    /// <summary>
    /// The same sum, with the seat's view handed in — the form a test can ask a question of.
    /// </summary>
    /// <param name="money">Which values pay this round, and how much (RULES.md §4.1).</param>
    /// <param name="seen">
    /// The cards this seat has in front of it. <b>Distinct physical cards</b>: each is
    /// subtracted from the shoe once, so naming one twice would understate what is left.
    /// </param>
    /// <param name="stakes">What the round is played for.</param>
    /// <param name="players">Everybody at the table — a money card collects from each of the others.</param>
    /// <remarks>
    /// <b>A card's own multiplier is what leaves the pool with it</b>, so the expected multiplier
    /// of an unseen card is the shoe's total less what this seat can see, over the count of what
    /// is left. No enumeration of designators is needed and none is exposed:
    /// <see cref="MoneyCardRegistry"/> answers by value and stays the only authority on which
    /// cards pay.
    /// </remarks>
    internal static double PerBlindDraw(
        MoneyCardRegistry money,
        IReadOnlyList<Card> seen,
        Stakes stakes,
        int players)
    {
        ArgumentNullException.ThrowIfNull(money);
        ArgumentNullException.ThrowIfNull(seen);
        ArgumentNullException.ThrowIfNull(stakes);

        var unseen = Shoe.Count - seen.Count;

        if (unseen <= 0 || players < 2)
        {
            return 0;
        }

        var inTheShoe = 0;

        foreach (var card in Shoe)
        {
            inTheShoe += money.Multiplier(card);
        }

        foreach (var card in seen)
        {
            inTheShoe -= money.Multiplier(card);
        }

        // Never negative: a seat cannot have been shown more money than the shoe holds, but a
        // clamp is what stops a miscount inverting a comparison rather than merely skewing it.
        return Math.Max(0, inTheShoe) / (double)unseen * stakes.MoneyCardValue * (players - 1);
    }
}
