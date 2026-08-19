namespace BurmesePoker.Domain.Play;

/// <summary>
/// How a player takes their one card at the start of a turn (RULES.md §5).
/// </summary>
/// <remarks>
/// There are exactly two ways, and taking the turned-up money card is not one of them: that
/// is offered once, on the opening turn only, through
/// <see cref="Abstractions.IPlayerAgent.ClaimTurnedUpMoneyCard"/> (RULES.md §4.5). The two
/// members differ in more than provenance — a blind draw comes from the deck and so confers
/// ownership, while a card taken from a discard pile never does (RULES.md §4.4).
/// </remarks>
public enum TurnAction
{
    /// <summary>Take the previous player's most recent discard. Confers no ownership.</summary>
    TakeDiscard,

    /// <summary>Take the top of the draw pile, unseen. The deck gives it, so it is owned.</summary>
    DrawFromDeck
}
