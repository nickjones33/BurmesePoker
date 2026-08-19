using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>
/// One run: who is playing, how many games of how many rounds, and from which seed.
/// </summary>
public sealed record SimulationOptions
{
    /// <summary>The strategies being compared. Seats are dealt out of this list by rotation.</summary>
    public required IReadOnlyList<Strategy> Strategies { get; init; }

    /// <summary>How many seats at the table (RULES.md §2.1: four to six).</summary>
    public int Seats { get; init; } = RoundEngine.MinimumPlayers;

    /// <summary>How many independent games to play.</summary>
    public int Games { get; init; } = 100;

    /// <summary>How many rounds each game plays, with the banks carrying over (RULES.md §7.2).</summary>
    public int RoundsPerGame { get; init; } = 1;

    /// <summary>The one number the whole run is reproducible from.</summary>
    public int MasterSeed { get; init; } = 20260818;

    /// <summary>What every round is played for.</summary>
    public Stakes Stakes { get; init; } = Stakes.Standard;

    /// <summary>
    /// How many turns a round may run before the harness gives up on it. Generous by default:
    /// P10 measured a bot round at 21–30 turns, so this fires only on a strategy that has
    /// genuinely stopped making progress.
    /// </summary>
    public int TurnCap { get; init; } = 400;

    /// <summary>Whether to play the games concurrently. Results are identical either way.</summary>
    public bool Parallel { get; init; } = true;

    /// <summary>Caps the concurrency when set; the scheduler decides when it is not.</summary>
    public int? MaxDegreeOfParallelism { get; init; }

    /// <summary>
    /// Whether every game keeps a journal, and how much of one (BUILD-PLAN §3.9). Null is off,
    /// which is the default.
    /// </summary>
    /// <remarks>
    /// <b>Off by default, and <see cref="JournalFidelity.Rich"/> off harder.</b> A journal is
    /// held in memory until the run ends, and the rich form copies a hand at every decision —
    /// §3.7 measured this work to be allocation-bound, so that is the expensive axis, not the
    /// cheap one. A throughput run wants neither; an analysis run asks for what it needs.
    /// </remarks>
    public JournalFidelity? Journal { get; init; }

    /// <summary>
    /// Which strategy sits in which seat for a given game.
    /// </summary>
    /// <remarks>
    /// <b>Rotated by game, because the seats are not equivalent.</b> Turn order is fixed for a
    /// round and seat 0 opens every one of them — it is the seat offered the turned-up money
    /// card, and it draws first. A comparison that left a strategy in seat 0 for every game
    /// would measure the seat as much as the strategy.
    /// </remarks>
    public Strategy[] Seating(int game)
    {
        var seating = new Strategy[Seats];

        for (var seat = 0; seat < Seats; seat++)
        {
            seating[seat] = Strategies[(seat + game) % Strategies.Count];
        }

        return seating;
    }

    /// <summary>Throws unless this run can actually be played.</summary>
    public SimulationOptions Validated()
    {
        ArgumentNullException.ThrowIfNull(Strategies);
        ArgumentNullException.ThrowIfNull(Stakes);

        if (Strategies.Count == 0)
        {
            throw new ArgumentException("A run needs at least one strategy.", nameof(Strategies));
        }

        if (Seats is < RoundEngine.MinimumPlayers or > RoundEngine.MaximumPlayers)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Seats),
                Seats,
                $"A round is for {RoundEngine.MinimumPlayers} to {RoundEngine.MaximumPlayers} players (RULES.md §2.1).");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(Games, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(RoundsPerGame, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(TurnCap, 1);

        return this;
    }
}
