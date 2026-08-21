using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Server;

/// <summary>
/// What a connection sends back in reply to a <see cref="SeatPrompt"/>.
/// </summary>
/// <remarks>
/// <para>
/// One case per <see cref="SeatQuestion"/>, and each one knows how to check itself against the
/// prompt it claims to answer. <b>An answer that does not fit is refused rather than played</b>
/// — <see cref="SeatConnection.Answer"/> returns false, the prompt stays pending, and the
/// client may try again. A bad answer is a bug in a client, and a client bug must not be able
/// to end a table's round.
/// </para>
/// <para>
/// The engine validates too and always did (P7): a discard that is not in the hand is rejected
/// and a declaration is only ever offered on a hand that wins. This is the same rule stated
/// one layer out, where it can be answered with <em>"no, try again"</em> instead of an
/// exception.
/// </para>
/// <para>
/// A closed hierarchy — the constructor is private, so a consumer can switch exhaustively.
/// </para>
/// </remarks>
public abstract record SeatAnswer
{
    private SeatAnswer()
    {
    }

    /// <summary>Whether this answer fits the question that was asked.</summary>
    public abstract bool Fits(SeatPrompt prompt);

    /// <summary>Take the discard, or draw blind.</summary>
    public sealed record Take(TurnAction Action) : SeatAnswer
    {
        public override bool Fits(SeatPrompt prompt) =>
            prompt is { Question: SeatQuestion.Take } && Enum.IsDefined(Action);
    }

    /// <summary>Whether to claim the top turned-up money card (RULES.md §4.5).</summary>
    public sealed record Claim(bool Claimed) : SeatAnswer
    {
        public override bool Fits(SeatPrompt prompt) => prompt is { Question: SeatQuestion.ClaimMoneyCard };
    }

    /// <summary>
    /// Whether to refuse the seat after you the turned-up money card (RULES.md §4.5).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Both answers fit.</b> The table asks only a seat that is holding the rank, so there
    /// is nothing here to check against the prompt that the prompt has not already decided — the
    /// same shape as <see cref="Claim"/>, and for the same reason.
    /// </remarks>
    public sealed record Objection(bool Objected) : SeatAnswer
    {
        public override bool Fits(SeatPrompt prompt) => prompt is { Question: SeatQuestion.ObjectToClaim };
    }

    /// <summary>
    /// The card to throw away. Must be one the seat is holding <b>and</b> one the turn offered
    /// (RULES.md §5.1).
    /// </summary>
    public sealed record Discard(Card Card) : SeatAnswer
    {
        public override bool Fits(SeatPrompt prompt) =>
            prompt is { Question: SeatQuestion.Discard } && prompt.MayThrow(Card);
    }

    /// <summary>Whether to lay all thirteen down and end the round (RULES.md §7.1).</summary>
    public sealed record Declaration(bool Declared) : SeatAnswer
    {
        public override bool Fits(SeatPrompt prompt) => prompt is { Question: SeatQuestion.Declare };
    }
}
