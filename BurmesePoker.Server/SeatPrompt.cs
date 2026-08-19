using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;

namespace BurmesePoker.Server;

/// <summary>
/// A question put to one seat, with everything that seat may see in order to answer it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A <see cref="TurnContext"/> that has stopped being live.</b> The context is the
/// concealment rule expressed as a type (P7) — there is no route from it to another hand, to
/// the table, or to the ownership record — so this carries the same fields and adds nothing.
/// What it changes is <em>time</em>: a context is a window onto a round in progress, and this
/// is a photograph of it.
/// </para>
/// <para>
/// 🔥 <b>Every member is a snapshot, and P13.1 is why.</b> <c>TurnContext.Hand</c> is the seat's
/// own live list and <c>RoundEngine</c> discards from it the moment the answer comes back, so a
/// view that kept the reference reported fourteen cards while holding thirteen. The console
/// never showed it because it draws and drops in one breath; <b>a connection holds what it was
/// given until it next renders</b>, which is exactly the case that does.
/// <see cref="HandView"/> copies (P13.1) and <see cref="TurnedUpMoneyCards"/> is copied here —
/// it is <c>TableState.TurnedUpOnTable</c>, which loses a card the moment the opening player
/// claims one.
/// </para>
/// </remarks>
/// <param name="Player">Whose seat is being asked.</param>
/// <param name="Question">Which of the four questions this is.</param>
/// <param name="Round">Which round of the match, counting from 1.</param>
/// <param name="TurnNumber">Which turn of the round, counting from 1.</param>
/// <param name="AvailableDiscard">The previous player's top discard, or null on the opening turn.</param>
/// <param name="CanClaimTurnedUpMoneyCard">Whether the top money card may be claimed (RULES.md §4.5).</param>
/// <param name="Taken">The card taken this turn, or null before it has been taken.</param>
/// <param name="CanDeclare">Whether these thirteen cards win (RULES.md §7.1).</param>
/// <param name="DrawPileCount">How many cards are left to draw.</param>
/// <param name="TurnedUpMoneyCards">The turned-up cards still on the table. A copy.</param>
/// <param name="MoneyCards">Which values pay this round, and how much. Immutable by construction (§3.3).</param>
/// <param name="Stakes">What the round is played for.</param>
/// <param name="Hand">
/// The seat's own hand as a view model, with the computer's suggested throw marked when the
/// table is offering hints. ⚠️ <b>The suggestion comes from <see cref="ComputerAdvice"/></b> and
/// is never re-derived (P13.1).
/// </param>
public sealed record SeatPrompt(
    PlayerId Player,
    SeatQuestion Question,
    int Round,
    int TurnNumber,
    Card? AvailableDiscard,
    bool CanClaimTurnedUpMoneyCard,
    Card? Taken,
    bool CanDeclare,
    int DrawPileCount,
    IReadOnlyList<Card> TurnedUpMoneyCards,
    MoneyCardRegistry MoneyCards,
    Stakes Stakes,
    HandView Hand)
{
    /// <summary>Photographs the turn as the asked seat may see it.</summary>
    /// <param name="context">The decision being put. Only the asking seat's own view is read.</param>
    /// <param name="question">Which of the four questions it is.</param>
    /// <param name="suggestedThrow">The computer's answer, or null when hints are off.</param>
    public static SeatPrompt For(TurnContext context, SeatQuestion question, Card? suggestedThrow = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new SeatPrompt(
            context.Player,
            question,
            context.Round,
            context.TurnNumber,
            context.AvailableDiscard,
            context.CanClaimTurnedUpMoneyCard,
            context.Taken,
            // Asked of the context rather than recomputed: HandEvaluator is the only authority
            // on a winning hand, and the engine asks it the same question (RULES.md §7.1).
            context.CanDeclare,
            context.DrawPileCount,
            // Copied. This is the table's own list and a claim removes a card from it.
            [.. context.TurnedUpMoneyCards],
            context.MoneyCards,
            context.Stakes,
            HandView.Of(context, suggestedThrow));
    }

    /// <summary>Whether this seat is holding the given card — by instance, not value (§3.1).</summary>
    public bool Holds(Card card) => Hand.Hand.Any(held => held.Id == card.Id);
}
