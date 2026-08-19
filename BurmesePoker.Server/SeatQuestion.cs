namespace BurmesePoker.Server;

/// <summary>
/// Which of <see cref="Domain.Abstractions.IPlayerAgent"/>'s four questions a seat is being
/// asked.
/// </summary>
/// <remarks>
/// One case per interface method and no more. The interface has not moved since P7 and this
/// is deliberately not an opportunity to extend it: a remote player answers exactly what a bot
/// answers, which is what makes a browser seat cost the domain nothing (BUILD-PLAN §3.6).
/// </remarks>
public enum SeatQuestion
{
    /// <summary>Take the previous player's discard, or draw blind?</summary>
    Take,

    /// <summary>Take the top turned-up money card instead of drawing (RULES.md §4.5)?</summary>
    ClaimMoneyCard,

    /// <summary>Which of the 14 held cards to throw away.</summary>
    Discard,

    /// <summary>Lay down all 13 and end the round (RULES.md §7.1)?</summary>
    Declare
}
