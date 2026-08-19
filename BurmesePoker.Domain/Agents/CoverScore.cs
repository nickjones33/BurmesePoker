using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// The one question every bot asks: <em>of the cards I would be left holding, how many meld?</em>
/// </summary>
/// <remarks>
/// Shared rather than copied, for the reason <c>MeldIndex</c> is shared between the two cover
/// searches: two copies of "does this card improve my hand" would be two places for the
/// answer to drift. A strategy's character lives in what it does with the score — the
/// tie-breaks, the risk it takes — not in the score itself.
/// </remarks>
internal static class CoverScore
{
    /// <summary>How many of these cards a best partial cover accounts for.</summary>
    internal static int Covered(IReadOnlyList<Card> hand) => PartialCover.Best(hand).CoveredCount;

    /// <summary>
    /// Would taking this card raise the count?
    /// </summary>
    /// <remarks>
    /// <b>Asked of the fourteen rather than of the thirteen that would be kept</b> — the same
    /// answer for a fourteenth of the work, because any improvement must use the new card, and
    /// every fourteen-card arrangement has a meld of four or more to give a card back from.
    /// </remarks>
    internal static bool Improves(IReadOnlyList<Card> hand, Card card) =>
        Covered([.. hand, card]) > Covered(hand);

    /// <summary>The hand without that exact card — instance identity, not value (BUILD-PLAN §3.1).</summary>
    internal static List<Card> Without(IReadOnlyList<Card> hand, Card card)
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
