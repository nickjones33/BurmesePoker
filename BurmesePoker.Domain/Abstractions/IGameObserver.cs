using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Abstractions;

/// <summary>
/// Narration. The engine tells an observer what happened; it never asks one anything.
/// </summary>
/// <remarks>
/// <para>
/// Every method is a no-op by default, so a front end implements only the events it draws.
/// </para>
/// <para>
/// <b>The engine narrates everything, including private information</b> — a blind draw
/// reports the card. Deciding what a given viewer may see is the front end's job; the
/// alternative, filtering per observer down here, would put presentation in the domain.
/// </para>
/// </remarks>
public interface IGameObserver
{
    /// <summary>The deal is done and the money cards are turned up.</summary>
    void RoundStarted(int round, IReadOnlyList<Card> turnedUp) { }

    /// <summary>A player drew blind from the deck. The deck gave it, so they own it.</summary>
    void PlayerDrew(PlayerId player, Card card) { }

    /// <summary>A player took the previous player's discard. Public, and confers no ownership.</summary>
    void PlayerTookDiscard(PlayerId player, Card card) { }

    /// <summary>The opening player took the top money card off the table (RULES.md §4.5).</summary>
    void MoneyCardClaimed(PlayerId player, Card card) { }

    /// <summary>A player discarded. Ownership is unaffected (RULES.md §4.4).</summary>
    void PlayerDiscarded(PlayerId player, Card card) { }

    /// <summary>
    /// The draw pile ran out, so every discard pile was gathered and shuffled into a new one
    /// (RULES.md §5). Rare — most rounds end first.
    /// </summary>
    void DiscardsReshuffled(int cards) { }

    /// <summary>A player laid down all 13 cards and ended the round (RULES.md §7.1).</summary>
    void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds) { }

    /// <summary>The money has been worked out.</summary>
    void RoundSettled(RoundResult result) { }
}
