namespace BurmesePoker.Sim;

/// <summary>
/// A round ran past the harness's turn cap and was given up on.
/// </summary>
/// <remarks>
/// <b>The cap belongs here and not in the domain.</b> Only a declaration ends a round
/// (RULES.md §7.1) and the reshuffle keeps the cards moving, so a strategy that stops
/// improving plays for ever; a limit in the engine would be inventing a rule the game does
/// not have (BUILD-PLAN P12). So the harness bounds a round from the outside, through the one
/// seam it already has — the agent — and <b>reports how many rounds it abandoned</b>, because
/// a strategy that stalls is a result rather than an error.
/// </remarks>
public sealed class RoundAbandonedException(int round, int turn, int cap)
    : Exception($"Round {round} reached turn {turn}, past the harness cap of {cap}.")
{
    /// <summary>Which round of the match stalled.</summary>
    public int Round { get; } = round;

    /// <summary>The turn it was abandoned on.</summary>
    public int Turn { get; } = turn;

    /// <summary>The cap that was exceeded.</summary>
    public int Cap { get; } = cap;
}
