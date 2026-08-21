namespace BurmesePoker.Server;

/// <summary>
/// Which of <see cref="Domain.Abstractions.IPlayerAgent"/>'s five questions a seat is being
/// asked.
/// </summary>
/// <remarks>
/// <para>
/// One case per interface method and no more. A remote player answers exactly what a bot
/// answers, which is what makes a browser seat cost the domain nothing (BUILD-PLAN §3.6) — so
/// this grows when the interface grows and never otherwise.
/// </para>
/// <para>
/// ⚠️ <b><see cref="ObjectToClaim"/> is the first one asked of a seat that is not on turn</b>
/// (P28). Everything a prompt carries is still that seat's own view, so nothing else about the
/// conversation changes; what changes is that a question can arrive while somebody else is
/// playing.
/// </para>
/// </remarks>
public enum SeatQuestion
{
    /// <summary>Take the previous player's discard, or draw blind?</summary>
    Take,

    /// <summary>Take the top turned-up money card instead of drawing (RULES.md §4.5)?</summary>
    ClaimMoneyCard,

    /// <summary>
    /// Refuse the seat after you the turned-up money card, which you may do only by holding
    /// that rank (RULES.md §4.5). Asked while it is not your turn.
    /// </summary>
    ObjectToClaim,

    /// <summary>Which of the 14 held cards to throw away.</summary>
    Discard,

    /// <summary>Lay down all 13 and end the round (RULES.md §7.1)?</summary>
    Declare
}
