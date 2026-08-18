namespace BurmesePoker.Domain.Cards;

/// <summary>
/// The rank of a ranked card. Jokers are rankless — they are represented by a
/// <c>null</c> rank rather than by a member of this enum, so that joker-adjacency
/// arithmetic cannot happen (BUILD-PLAN §3.2, root cause C).
/// </summary>
/// <remarks>
/// The numeric values are the run-ordering values: consecutive ranks differ by one, and
/// Ace is high at 14. Ace-low runs (A-2-3) are handled by the run generator treating Ace
/// as 1 at the start of a run only; a run may never pass through Ace in the middle
/// (RULES.md §6.1).
/// </remarks>
public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}
