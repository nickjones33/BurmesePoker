using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Domain.Melds;

/// <summary>
/// One position in a meld: the physical card occupying it, and the rank and suit that card
/// is <i>playing as</i>.
/// </summary>
/// <remarks>
/// For a ranked card the played rank and suit are always its own. For a joker they are the
/// card it is standing in for — the "representative interpretation" of
/// <c>docs/spec/RUN-CANDIDATES.md</c> §2. The interpretation is for display and auditing
/// only: it must never take part in meld identity or de-duplication, because
/// <c>{2♦,3♦,🃏}</c> consumes the same three cards whether the joker plays the 4♦ or the A♦.
/// </remarks>
public readonly record struct MeldSlot(Card Card, Rank PlaysAs, Suit InSuit)
{
    /// <summary>True when a joker is standing in for the rank and suit of this position.</summary>
    public bool IsSubstitute => Card.IsJoker;

    public override string ToString() =>
        IsSubstitute
            ? $"{Card}(as {CardText.DisplayCode(PlaysAs)}{CardText.DisplaySuit(InSuit)})"
            : Card.ToString();
}
