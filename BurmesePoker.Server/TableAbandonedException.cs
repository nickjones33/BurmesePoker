namespace BurmesePoker.Server;

/// <summary>
/// The round ran past the table's wall-clock limit, so the host gave up on it.
/// </summary>
/// <remarks>
/// <b>Nothing in the rules ends a round but a declaration</b> (RULES.md §7.1), and with the
/// reshuffle in place (P9) the cards circulate for ever — so a table whose people all walk away
/// plays until it is killed. P12 bounds a simulated round by turn count and reports it;
/// <b>a host bounds a real one by the clock</b>, because the thing that has gone wrong is
/// people rather than play. Sibling in spirit to <c>BurmesePoker.Sim.RoundAbandonedException</c>
/// and deliberately not shared with it: the two measure different things.
/// </remarks>
public sealed class TableAbandonedException(int round, TimeSpan limit)
    : Exception($"Round {round} ran past the table's limit of {limit}, so it was abandoned.")
{
    /// <summary>Which round was given up on.</summary>
    public int Round { get; } = round;

    /// <summary>The limit it ran past.</summary>
    public TimeSpan Limit { get; } = limit;
}
