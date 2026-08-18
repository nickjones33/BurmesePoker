namespace BurmesePoker.Domain.Melds;

/// <summary>
/// The two kinds of meld: a run of contiguous ranks in one suit, or a set of one rank
/// across distinct suits (RULES.md §6).
/// </summary>
public enum MeldKind
{
    Run,
    Set
}
