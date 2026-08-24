using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// The restraint half of playing RULES.md §5.1 at somebody: the legal discards a seat is still
/// <b>willing</b> to make, once the ranks it has locked against the seat above are held back
/// (BUILD-PLAN P31, shared by P43).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>One home, two rungs.</b> <see cref="WardenBotAgent"/> pays for its locks with a draw
/// and <see cref="OpportunistBotAgent"/> takes only what it wanted anyway — but the hold is the
/// same idea in both: a lock this seat armed is worth nothing if this seat then releases it
/// (§5.1, exception 1 — a release is permanent). Two copies of the hold would be two places for
/// the escapes below to drift apart, which is P28's "one predicate, never written twice" applied
/// to a strategy rather than a rule.
/// </para>
/// <para>
/// 🔥 <b>Deliberately the same shape as <see cref="FeedingBan.LegalDiscards"/></b>, because it
/// is the same rule read from the other side: the seat holds those ranks the way the rule binds
/// the seat above, not more strictly than that.
/// </para>
/// <para>
/// ⚠️ <b>Going out first.</b> Where the throw would be the declaring discard the lock yields:
/// the round ends on it and the protected player never gets a turn in which to take the card
/// (§5.1, exception 2). <see cref="PartialCover.CoversAtLeast"/> gates the expensive question,
/// exactly as the ban's own enforcement does.
/// </para>
/// <para>
/// ⚠️ <b>And a floor.</b> Where holding every locked rank would leave nothing to throw, the seat
/// throws anyway — the discard is mandatory (§7.1) and a self-imposed restraint least of all
/// outranks it.
/// </para>
/// </remarks>
internal static class HeldLocks
{
    /// <summary>The legal discards this seat is actually willing to make.</summary>
    internal static IReadOnlyList<Card> Candidates(TurnContext context) =>
        Willing(context, context.LegalDiscards);

    /// <summary>
    /// That choice less the ranks this seat has locked — <b>never empty</b>, and with the two
    /// escapes RULES.md §5.1 gives itself.
    /// </summary>
    internal static IReadOnlyList<Card> Willing(TurnContext context, IReadOnlyList<Card> candidates)
    {
        var locked = context.ClosedByYou;

        if (locked.IsEmpty)
        {
            return candidates;
        }

        var willing = new List<Card>(candidates.Count);
        var held = 0;

        foreach (var card in candidates)
        {
            if (locked.Closes(card))
            {
                held++;
            }
            else
            {
                willing.Add(card);
            }
        }

        if (held == 0)
        {
            return candidates;
        }

        if (context.Hand.Count == RoundEngine.HandSize + 1
            && PartialCover.CoversAtLeast(context.Hand, RoundEngine.HandSize))
        {
            var withTheWinningDiscard = new List<Card>(candidates.Count);

            foreach (var card in candidates)
            {
                if (!locked.Closes(card)
                    || HandEvaluator.IsWinning(CoverScore.Without(context.Hand, card), context.Rules))
                {
                    withTheWinningDiscard.Add(card);
                }
            }

            willing = withTheWinningDiscard;
        }

        return willing.Count > 0 ? willing : candidates;
    }
}
