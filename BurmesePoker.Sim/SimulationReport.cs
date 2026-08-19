namespace BurmesePoker.Sim;

/// <summary>
/// What a run produced: every game, and the per-strategy totals over them.
/// </summary>
/// <param name="Options">The run's inputs — the master seed among them, so a report is self-describing.</param>
/// <param name="Games">Every game, in index order, each carrying its own seed and seating.</param>
/// <param name="Rounds">How many rounds actually settled.</param>
/// <param name="Turns">Turns across all of them.</param>
/// <param name="Reshuffles">How often a draw pile ran out and was rebuilt (RULES.md §5).</param>
/// <param name="AbandonedGames">
/// Games stopped by the turn cap. <b>Reported rather than hidden</b>: a strategy that stalls is
/// a finding, and a run that quietly dropped a tenth of its games would not be one.
/// </param>
/// <param name="Strategies">Per-strategy totals, in the order the run named them.</param>
/// <param name="Elapsed">Wall-clock time, which is the only field that changes between two identical runs.</param>
public sealed record SimulationReport(
    SimulationOptions Options,
    IReadOnlyList<GameResult> Games,
    int Rounds,
    long Turns,
    int Reshuffles,
    int AbandonedGames,
    IReadOnlyList<StrategySummary> Strategies,
    TimeSpan Elapsed)
{
    /// <summary>Rounds settled per second of wall clock.</summary>
    public double RoundsPerSecond => Elapsed.TotalSeconds <= 0 ? 0 : Rounds / Elapsed.TotalSeconds;

    /// <summary>Average turns to a declaration.</summary>
    public double TurnsPerRound => Rounds == 0 ? 0 : (double)Turns / Rounds;
}
