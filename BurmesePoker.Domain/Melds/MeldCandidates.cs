using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// Every meld a hand could make, of either kind: the union of <see cref="RunGenerator"/> and
/// <see cref="SetGenerator"/> (BUILD-PLAN §3.4).
/// </summary>
/// <remarks>
/// <para>
/// The candidates <b>deliberately overlap</b> — the same joker instance is offered to a
/// diamond run and a heart run, and every sub-run of a longer run appears. That is correct
/// for a candidate generator; disjointness is <see cref="HandEvaluator"/>'s job, enforced by
/// <see cref="Meld.CardIds"/>.
/// </para>
/// <para>
/// The union is <b>not a naive concatenation</b>. The two generators emit the same card set
/// whenever a meld holds at most one real card: <c>{9♦,🃏,🃏}</c> is a run (the jokers play
/// the 10♦ and J♦) <i>and</i> a set (they play the 9♠ and 9♥), and <c>{🃏,🃏,🃏}</c> is both
/// trivially. Since a meld's identity is the cards it consumes, those are one candidate, not
/// two — keeping both would make the cover search try the same cover twice and would let
/// <see cref="Meld.Kind"/> come out arbitrarily. The <b>run</b> interpretation is kept,
/// because runs are generated first.
/// </para>
/// </remarks>
public static class MeldCandidates
{
    /// <summary>
    /// Every distinct meld the hand can make, by set of cards consumed: runs first, then the
    /// sets that consume a card set no run already covers.
    /// </summary>
    public static IReadOnlyList<Meld> For(IReadOnlyList<Card> hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        var runs = RunGenerator.Candidates(hand);
        var sets = SetGenerator.Candidates(hand);

        var candidates = new List<Meld>(runs.Count + sets.Count);
        var seen = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());

        // Each generator has already de-duplicated within itself, so every run is new.
        foreach (var run in runs)
        {
            seen.Add(run.IdentityKey);
            candidates.Add(run);
        }

        foreach (var set in sets)
        {
            if (seen.Add(set.IdentityKey))
            {
                candidates.Add(set);
            }
        }

        return candidates;
    }
}
