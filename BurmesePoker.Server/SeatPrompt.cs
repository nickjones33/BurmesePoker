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
/// <param name="Question">Which of the five questions this is.</param>
/// <param name="Round">Which round of the match, counting from 1.</param>
/// <param name="TurnNumber">
/// Which turn of the round, counting from 1 — and <b>0</b> for the one question that is not asked
/// during a turn: whether to lay down a thirteen that already won on the deal (RULES.md §7.4).
/// </param>
/// <param name="AvailableDiscard">The previous player's top discard, or null on the opening turn.</param>
/// <param name="CanClaimTurnedUpMoneyCard">Whether the top money card may be claimed (RULES.md §4.5).</param>
/// <param name="PermissionAsked">
/// The claim this seat is being asked to permit, or null — which is every question but
/// <see cref="SeatQuestion.ObjectToClaim"/> (RULES.md §4.5).
/// </param>
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
/// <param name="Rationale">
/// <b>Why</b> the computer would answer this question the way it would, or null when the table
/// is not offering hints (P24.2).
/// </param>
public sealed record SeatPrompt(
    PlayerId Player,
    SeatQuestion Question,
    int Round,
    int TurnNumber,
    Card? AvailableDiscard,
    bool CanClaimTurnedUpMoneyCard,
    ClaimRequest? PermissionAsked,
    Card? Taken,
    bool CanDeclare,
    int DrawPileCount,
    IReadOnlyList<Card> TurnedUpMoneyCards,
    MoneyCardRegistry MoneyCards,
    Stakes Stakes,
    HandView Hand,
    AdviceRationale? Rationale = null)
{
    /// <summary>Photographs the turn as the asked seat may see it.</summary>
    /// <param name="context">The decision being put. Only the asking seat's own view is read.</param>
    /// <param name="question">Which of the five questions it is.</param>
    /// <param name="suggestedThrow">The computer's answer, or null when hints are off.</param>
    /// <param name="rationale">
    /// Why it would answer that way, or null when hints are off. ⚠️ <b>It rides here and never on
    /// a <c>TableEvent</c></b>: this record is seat-private by construction, and one seat's
    /// reasoning on the bus is precisely the leak <c>ConcealmentTests</c> exists to catch
    /// (BUILD-PLAN §3.11 A1).
    /// </param>
    /// <param name="faceUp">
    /// This seat's own face-up cards — taken in the open and still held, visible to the whole
    /// table (RULES.md §5.2) — or null when nothing is. ⚠️ <b>Read off the fan-out's
    /// <c>TableLook</c> rather than off the context</b> (P41): the face-up set is a fold over
    /// public events, and <c>TurnContext</c> is deliberately not widened to carry it.
    /// </param>
    public static SeatPrompt For(
        TurnContext context,
        SeatQuestion question,
        Card? suggestedThrow = null,
        AdviceRationale? rationale = null,
        IReadOnlyList<Card>? faceUp = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new SeatPrompt(
            context.Player,
            question,
            context.Round,
            context.TurnNumber,
            context.AvailableDiscard,
            context.CanClaimTurnedUpMoneyCard,
            context.PermissionAsked,
            context.Taken,
            // Asked of the context rather than recomputed: HandEvaluator is the only authority
            // on a winning hand, and the engine asks it the same question (RULES.md §7.1).
            context.CanDeclare,
            context.DrawPileCount,
            // Copied. This is the table's own list and a claim removes a card from it.
            [.. context.TurnedUpMoneyCards],
            context.MoneyCards,
            context.Stakes,
            HandView.Of(context, suggestedThrow, faceUp),
            rationale);
    }

    /// <summary>
    /// Whether this seat could refuse the claim it is being asked about — it holds that rank
    /// (RULES.md §4.5, §9 #30). False when nothing is being asked.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Not a check on the answer, and there is deliberately none.</b> The table asks only a
    /// seat that may refuse, so by the time this prompt exists the question is already free; this
    /// is here so a client can say <em>why</em> it is being offered a veto, which is the difference
    /// between a control and a control with a reason (§3.11 C13).
    /// </remarks>
    public bool MayObject => PermissionAsked is { } claim && claim.MayBeRefusedBy(Hand.Hand);

    /// <summary>Whether this seat is holding the given card — by instance, not value (§3.1).</summary>
    public bool Holds(Card card) => Hand.Hand.Any(held => held.Id == card.Id);

    /// <summary>
    /// Whether the turn actually offered this card to throw — held, <b>and</b> not of a rank the
    /// seat below has taken in the open (RULES.md §5.1).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Refused here rather than in the engine.</b> A banned discard is an impossible move, so
    /// a remote seat naming one is answering a question it was not asked — the same class of thing
    /// as naming a card it is not holding, and it is refused the same way: the answer does not fit,
    /// the seat is still being asked, and the stand-in eventually plays. The engine's own guard
    /// stays where it is, for anything that gets past this.
    /// </remarks>
    public bool MayThrow(Card card) =>
        Hand.Hand.Any(held => held.Id == card.Id) && Hand.Of(card).CanBeThrown;
}
