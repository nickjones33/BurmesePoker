using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// What one seat has taken in the open, and so which ranks the seat that discards to it may not
/// throw for the rest of the round (RULES.md §5.1).
/// </summary>
/// <remarks>
/// <para>
/// <b>It belongs to the seat being protected, not to the seat being constrained.</b> The rule is
/// stated from the collector's side — <em>once a player has publicly taken a card, the player who
/// discards to them may not discard any card of the same rank</em> — and only one seat can feed
/// them, because only the immediately preceding seat's top discard is takeable (§5). So this is
/// one <see cref="PlayerState"/>'s own record, and the seat above reads it.
/// </para>
/// <para>
/// 🔥 <b>A banned discard is not an infraction but an impossible move</b> (RULES.md §5.1,
/// Enforcement; §10 #13). There is no penalty and nothing to retract, because the card is never
/// offered and cannot be chosen — which is why the whole of this type's output is
/// <see cref="LegalDiscards"/>, a list a turn picks from, rather than a validity check something
/// applies afterwards.
/// </para>
/// <para>
/// ⚠️ <b>The release is kept, not read back off the table.</b> A rank re-opens when the protected
/// player throws that rank back, and at a table that question is answered by looking down their
/// own discard pile — legitimately, since the discards are public (§9 #15). In code it must not
/// be: §5 sweeps every pile back into the draw pile when the deck runs out, which would take the
/// evidence away and silently re-arm a released rank mid-round (§9 #19, a <c>PLAYER</c> house
/// ruling). Two sets, no history: order and timing decide nothing.
/// </para>
/// <para>
/// <b>Everything here is per round.</b> A <see cref="TableState"/> is built by the deal and so is
/// every seat in it, which is what wipes the ban — the same reason a <c>CardId</c> names a card in
/// a round's shoe and not across rounds (P13.4).
/// </para>
/// </remarks>
public sealed class FeedingBan
{
    /// <summary>
    /// Ranks taken in the open and not since thrown back. <c>null</c> is the joker key — see
    /// <see cref="Card.SameRankAs"/>, which is the same equality in predicate form.
    /// </summary>
    private readonly HashSet<Rank?> _closed = [];

    /// <summary>
    /// Ranks this seat has thrown away this round. A rank in here can never close again, however
    /// many more of it the seat picks up (RULES.md §5.1, exception 1).
    /// </summary>
    private readonly HashSet<Rank?> _released = [];

    /// <summary>Whether anything at all is closed — the ordinary state of an ordinary turn.</summary>
    public bool IsEmpty => _closed.Count == 0;

    /// <summary>How many ranks are closed.</summary>
    public int Count => _closed.Count;

    /// <summary>
    /// Whether this card is of a rank the seat above may not throw. Rank alone: a Q♦ taken in the
    /// open closes every Queen, and a joker taken closes the other jokers (§9 #27).
    /// </summary>
    public bool Closes(Card card) => _closed.Contains(card.Rank);

    /// <summary>Whether this seat has thrown that rank back, which re-opens it for good.</summary>
    public bool HasReleased(Card card) => _released.Contains(card.Rank);

    /// <summary>
    /// The cards of that hand which may legally be thrown to this seat, in the hand's own order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Never empty, by construction</b> (RULES.md §5.1, The floor — <c>PLAYER</c>, Settled).
    /// The legal discards are the hand minus the closed ranks, <em>or the whole hand if that is
    /// empty</em>: the discard is mandatory (§7.1) and the ban is not, so where the ban would leave
    /// a player unable to move it yields for that turn. Impossible-move enforcement removes the
    /// social escape hatch, which is what makes the floor load-bearing rather than tidy — without
    /// it a hand of fourteen banned cards is a turn that cannot be completed. ⚠️ The yield is for
    /// that turn only and releases nothing.
    /// </para>
    /// <para>
    /// ⚠️ <b>Going out outranks the ban too</b> (§5.1, exception 2). A banned rank may be thrown as
    /// the declaring discard — the 14th card, thrown immediately before laying down all 13 — and it
    /// costs nothing, because the round ends on it and the protected player never gets a turn in
    /// which to take the card. That is the second candidate loop below, and
    /// <see cref="PartialCover.CoversAtLeast"/> gates it: if no thirteen of these cards could all
    /// meld, no discard here is a declaring discard and the expensive question is never asked.
    /// </para>
    /// <para>
    /// ✅ <b>The card just taken is filtered like any other and has no case of its own</b> (§9 #13):
    /// throwing it straight back is legal *"as long as you aren't violating any other discard
    /// rules"*, and §5.1 is the only other discard rule there is.
    /// </para>
    /// </remarks>
    /// <param name="hand">The cards held — fourteen, on the turn a discard is being chosen.</param>
    /// <param name="rules">What a declared hand must contain here, for exception 2 (§7.1.1).</param>
    public IReadOnlyList<Card> LegalDiscards(IReadOnlyList<Card> hand, TableRules rules)
    {
        ArgumentNullException.ThrowIfNull(hand);

        if (_closed.Count == 0)
        {
            return hand;
        }

        var legal = new List<Card>(hand.Count);
        var banned = 0;

        foreach (var card in hand)
        {
            if (Closes(card))
            {
                banned++;
            }
            else
            {
                legal.Add(card);
            }
        }

        if (banned == 0)
        {
            return hand;
        }

        // Exception 2, and it is asked only of a hand that could actually go out: the declaring
        // discard is the 14th card thrown immediately before laying down 13 (RULES.md §7.1), so a
        // hand of any other size is not deciding one — and CoversAtLeast is the cheap bar that
        // stops the search being run at all on the hands that are nowhere near.
        if (hand.Count == RoundEngine.HandSize + 1 && PartialCover.CoversAtLeast(hand, RoundEngine.HandSize))
        {
            var withTheWinningDiscard = new List<Card>(hand.Count);

            foreach (var card in hand)
            {
                if (!Closes(card) || HandEvaluator.IsWinning(Without(hand, card), rules))
                {
                    withTheWinningDiscard.Add(card);
                }
            }

            legal = withTheWinningDiscard;
        }

        // The floor. Nothing left to throw means the ban yields for this turn, not that the turn
        // cannot be completed.
        return legal.Count > 0 ? legal : hand;
    }

    /// <summary>
    /// This seat took a card where everybody could see it — the discard offered to it, or the
    /// turned-up money card claimed on the opening turn (RULES.md §4.5). That closes its rank.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A blind draw arms nothing</b> (§9 #17, <c>EXPERT</c>): the rule is about announcing
    /// what you are collecting, and a card off the deck announces nothing. ⚠️ <b>And a rank already
    /// released stays released</b> — throwing a Queen away is a public statement that you are not
    /// collecting Queens, and picking one up later does not retract it (§5.1, exception 1).
    /// </remarks>
    internal void TookInTheOpen(Card card)
    {
        if (!_released.Contains(card.Rank))
        {
            _closed.Add(card.Rank);
        }
    }

    /// <summary>
    /// This seat threw a card away, which re-opens its rank to whoever feeds them — for the rest of
    /// the round, and permanently (RULES.md §5.1, exception 1).
    /// </summary>
    internal void ThrewAway(Card card)
    {
        _released.Add(card.Rank);
        _closed.Remove(card.Rank);
    }

    /// <summary>The hand without that exact card — instance identity, not value (BUILD-PLAN §3.1).</summary>
    private static List<Card> Without(IReadOnlyList<Card> hand, Card card)
    {
        var kept = new List<Card>(hand.Count - 1);

        foreach (var held in hand)
        {
            if (held != card)
            {
                kept.Add(held);
            }
        }

        return kept;
    }
}
