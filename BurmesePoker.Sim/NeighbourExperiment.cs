using System.Diagnostics;

using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>Which side of the focal seat a cell varies.</summary>
public enum Neighbour
{
    /// <summary>The player who discards into the focal seat — the hypothesis's edge.</summary>
    Upstream,

    /// <summary>The player the focal seat discards into — the control.</summary>
    Downstream
}

/// <summary>
/// One run of the neighbour experiment: who is focal, what varies beside them, and how much.
/// </summary>
public sealed record NeighbourOptions
{
    /// <summary>The seat being measured. Held constant in every cell.</summary>
    public required Strategy Focal { get; init; }

    /// <summary>
    /// The settings of the dial. The <i>skill</i> levels are <c>random</c> / <c>simple</c> /
    /// <c>greedy</c> (P15 separated those by 36.9 and 12.9 points); <c>cautious</c> is the
    /// <b>intervention</b>, a strategy level with <c>greedy</c> that plays to deny.
    /// </summary>
    public required IReadOnlyList<Strategy> Levels { get; init; }

    /// <summary>
    /// Who fills the seats that are neither focal nor varied. <b>Held constant across every
    /// cell of both arms</b>, so that two cells differ in one seat's strategy and nothing else.
    /// </summary>
    public required Strategy Filler { get; init; }

    /// <summary>The level every other level is reported against. <c>greedy</c> by default.</summary>
    public required Strategy Reference { get; init; }

    /// <summary>Seats at the table (RULES.md §2.1).</summary>
    public int Seats { get; init; } = 4;

    /// <summary>
    /// Games in each cell. At a win rate near 30% this is what fixes the resolution: 2,000
    /// games gives a standard error near one point on a cell and about 1.5 on a difference,
    /// and 4,000 gives about 0.7 and 1.0.
    /// </summary>
    public int GamesPerCell { get; init; } = 2000;

    /// <summary>
    /// The one number the whole experiment reproduces from. <b>Shared by every cell on
    /// purpose</b> — game 417 is then dealt from the same shoe whichever neighbour is being
    /// varied, so the deal is held constant along with the rest of the table.
    /// </summary>
    public int MasterSeed { get; init; } = 20260819;

    /// <summary>Turns before a stalled round is given up on.</summary>
    public int TurnCap { get; init; } = 400;

    /// <summary>What the rounds are played for.</summary>
    public Stakes Stakes { get; init; } = Stakes.Standard;

    /// <summary>Whether the games inside a cell run concurrently. Results are identical either way.</summary>
    public bool Parallel { get; init; } = true;

    /// <summary>Throws unless the experiment can be run as described.</summary>
    public NeighbourOptions Validated()
    {
        ArgumentNullException.ThrowIfNull(Focal);
        ArgumentNullException.ThrowIfNull(Levels);
        ArgumentNullException.ThrowIfNull(Filler);
        ArgumentNullException.ThrowIfNull(Reference);

        if (Levels.Count < 2)
        {
            throw new ArgumentException("A dial with one setting measures nothing.", nameof(Levels));
        }

        if (!Levels.Any(level => level.Name == Reference.Name))
        {
            throw new ArgumentException(
                $"The reference level '{Reference.Name}' is not one of the levels.", nameof(Reference));
        }

        // Four seats or more, so that the seat before the focal one and the seat after it are
        // two different players — at three they would be the same seat and the arms would be
        // the same experiment.
        if (Seats < RoundEngine.MinimumPlayers || Seats > RoundEngine.MaximumPlayers)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Seats),
                Seats,
                $"A round is for {RoundEngine.MinimumPlayers} to {RoundEngine.MaximumPlayers} players (RULES.md §2.1).");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(GamesPerCell, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(TurnCap, 1);

        return this;
    }
}

/// <summary>
/// One cell: the focal seat's results with one strategy in one of its two neighbouring seats.
/// </summary>
/// <param name="Arm">Which neighbour was varied.</param>
/// <param name="Level">What was sitting there.</param>
/// <param name="Games">Games played.</param>
/// <param name="Settled">Games that reached a declaration and so produced a row.</param>
/// <param name="Abandoned">Games given up at the turn cap. Reported, never dropped (P12).</param>
/// <param name="FocalSeatGames">
/// How many settled games the focal seat spent in each seat index. <b>This is the balance
/// check</b> (P16 acceptance 2): the cell's games cycle the pattern round the table, so these
/// should be equal to within one.
/// </param>
/// <param name="WinRate">Share of settled games the focal seat declared in.</param>
/// <param name="TakeRate">
/// Share of the focal seat's card acquisitions that came off the upstream discard rather than
/// blind off the pile. <b>The mechanism variable</b>: if skill upstream travels to the focal
/// seat at all, this is the road it travels on.
/// </param>
/// <param name="NetPerRound">What the focal seat banked.</param>
/// <param name="CoveredWhenLosing">How many of the thirteen melded when it did not win.</param>
/// <param name="LevelWinRate">
/// The <i>varied</i> seat's own win rate — the other half of the intervention's prediction. A
/// denying strategy is supposed to cost its victim without costing itself.
/// </param>
public sealed record NeighbourCell(
    Neighbour Arm,
    string Level,
    int Games,
    int Settled,
    int Abandoned,
    IReadOnlyList<int> FocalSeatGames,
    Measurement WinRate,
    Measurement TakeRate,
    Measurement NetPerRound,
    Measurement CoveredWhenLosing,
    Measurement LevelWinRate);

/// <summary>One level's effect on the focal seat, against the reference level, within one arm.</summary>
public sealed record NeighbourEffect(
    Neighbour Arm,
    string Level,
    string Reference,
    Measurement WinRate,
    Measurement TakeRate,
    Measurement NetPerRound);

/// <summary>
/// The experiment: a focal seat, a dial in the seat before it, and the same dial in the seat
/// after it (BUILD-PLAN P16).
/// </summary>
/// <remarks>
/// <para>
/// <b>The control is the whole design.</b> Every cell seats the same four strategies — focal,
/// one varied, two fillers — so the two arms hold the table's <i>composition</i> exactly
/// constant and differ only in <b>which side of the focal seat the varied player sits on</b>.
/// Discards flow one way (RULES.md §5), so a hypothesis about the upstream edge predicts a
/// large upstream effect and a small downstream one. Without the second arm a result is only
/// "strong tables win more", which is not a claim about neighbours at all.
/// </para>
/// <para>
/// <b>Seat is averaged away rather than held.</b> Each cell cycles its games through the
/// <see cref="SeatingPlan.Rotations"/> of its pattern, so the focal seat sits in every seat
/// equally often. Rotating a cycle leaves every neighbour relation untouched and changes only
/// who opens — which is the one thing about a seat that is not a strategy.
/// </para>
/// </remarks>
public static class NeighbourExperiment
{
    /// <summary>Plays every cell and measures the focal seat in each.</summary>
    public static NeighbourReport Run(NeighbourOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validated();

        var clock = Stopwatch.StartNew();
        var cells = new List<NeighbourCell>(options.Levels.Count * 2);

        foreach (var arm in (Neighbour[])[Neighbour.Upstream, Neighbour.Downstream])
        {
            foreach (var level in options.Levels)
            {
                cells.Add(Cell(options, arm, level));
            }
        }

        clock.Stop();

        return new NeighbourReport(options, cells, clock.Elapsed);
    }

    /// <summary>
    /// The table a cell plays, with the focal seat opening: focal, then the rest of the cycle.
    /// </summary>
    /// <remarks>
    /// <b>Upstream is the last seat of the pattern and downstream is the second</b>, because
    /// turn order is seat order and the table wraps — the player before seat 0 is the player in
    /// the last seat.
    /// </remarks>
    public static Strategy[] Pattern(NeighbourOptions options, Neighbour arm, Strategy level)
    {
        var pattern = new Strategy[options.Seats];

        pattern[0] = options.Focal;

        for (var seat = 1; seat < options.Seats; seat++)
        {
            pattern[seat] = options.Filler;
        }

        pattern[VariedOffset(options, arm)] = level;

        return pattern;
    }

    /// <summary>How far round the cycle from the focal seat the varied seat sits.</summary>
    public static int VariedOffset(NeighbourOptions options, Neighbour arm) =>
        arm == Neighbour.Upstream ? options.Seats - 1 : 1;

    /// <summary>The run one cell is, ready to be played or inspected.</summary>
    public static SimulationOptions RunOf(NeighbourOptions options, Neighbour arm, Strategy level)
    {
        var pattern = Pattern(options, arm, level);

        // Distinct by name, because that is what the summary tabulates by and what an
        // assignment is validated against — seating the same strategy twice is normal here.
        var strategies = pattern
            .GroupBy(strategy => strategy.Name)
            .Select(group => group.First())
            .ToList();

        return new SimulationOptions
        {
            Strategies = strategies,
            Assignments = SeatingPlan.Rotations(pattern),
            Seats = options.Seats,
            Games = options.GamesPerCell,
            RoundsPerGame = 1,
            MasterSeed = options.MasterSeed,
            Stakes = options.Stakes,
            TurnCap = options.TurnCap,
            Parallel = options.Parallel
        }.Validated();
    }

    private static NeighbourCell Cell(NeighbourOptions options, Neighbour arm, Strategy level)
    {
        var report = Simulator.Run(RunOf(options, arm, level));

        var wins = new List<double>(options.GamesPerCell);
        var takes = new List<double>(options.GamesPerCell);
        var nets = new List<double>(options.GamesPerCell);
        var covered = new List<double>(options.GamesPerCell);
        var levelWins = new List<double>(options.GamesPerCell);
        var seatGames = new int[options.Seats];
        var settled = 0;
        var abandoned = 0;

        foreach (var game in report.Games)
        {
            if (game.Rounds.Count == 0)
            {
                abandoned++;
                continue;
            }

            if (game.Abandoned)
            {
                abandoned++;
            }

            // Games cycle the rotations in order, so the focal seat's index is arithmetic —
            // and asserted against the seating below rather than assumed.
            var focalSeat = game.Game % options.Seats;
            var levelSeat = (focalSeat + VariedOffset(options, arm)) % options.Seats;

            // ⚠️ Cheap, and it guards the one mistake that would be invisible in the output:
            // measuring the wrong seat produces a perfectly plausible set of numbers about
            // somebody else. The arithmetic above is only true because the cell's games cycle
            // the rotations in order, which is an agreement between two files.
            if (game.Seating[focalSeat] != options.Focal.Name)
            {
                throw new InvalidOperationException(
                    $"Game {game.Game} was expected to seat {options.Focal.Name} at seat {focalSeat}, "
                    + $"and seated {game.Seating[focalSeat]}.");
            }

            var round = game.Rounds[0];
            var focal = round.Seats[focalSeat];

            settled++;
            seatGames[focalSeat]++;

            wins.Add(focal.Won ? 1 : 0);
            nets.Add(focal.Net);
            levelWins.Add(round.Seats[levelSeat].Won ? 1 : 0);

            if (focal.Takes + focal.Draws > 0)
            {
                takes.Add((double)focal.Takes / (focal.Takes + focal.Draws));
            }

            if (!focal.Won)
            {
                covered.Add(focal.Covered);
            }
        }

        return new NeighbourCell(
            arm,
            level.Name,
            report.Games.Count,
            settled,
            abandoned,
            seatGames,
            Measurement.Of(wins),
            Measurement.Of(takes),
            Measurement.Of(nets),
            Measurement.Of(covered),
            Measurement.Of(levelWins));
    }
}

/// <summary>
/// What the experiment found: every cell, and the contrasts that answer the question.
/// </summary>
public sealed record NeighbourReport(
    NeighbourOptions Options,
    IReadOnlyList<NeighbourCell> Cells,
    TimeSpan Elapsed)
{
    /// <summary>One cell by arm and level.</summary>
    public NeighbourCell Cell(Neighbour arm, string level) =>
        Cells.FirstOrDefault(cell => cell.Arm == arm && cell.Level == level)
        ?? throw new ArgumentException($"No {arm.ToString().ToLowerInvariant()} cell for '{level}'.", nameof(level));

    /// <summary>
    /// Every level against the reference, in both arms — the upstream half is the hypothesis
    /// and the downstream half is the control.
    /// </summary>
    public IReadOnlyList<NeighbourEffect> Effects =>
    [
        .. Cells
            .Where(cell => cell.Level != Options.Reference.Name)
            .Select(cell =>
            {
                var reference = Cell(cell.Arm, Options.Reference.Name);

                return new NeighbourEffect(
                    cell.Arm,
                    cell.Level,
                    Options.Reference.Name,
                    Measurement.Difference(cell.WinRate, reference.WinRate),
                    Measurement.Difference(cell.TakeRate, reference.TakeRate),
                    Measurement.Difference(cell.NetPerRound, reference.NetPerRound));
            })
    ];

    /// <summary>
    /// The upstream effect of a level <i>less</i> its downstream effect — the number the
    /// hypothesis actually predicts.
    /// </summary>
    /// <remarks>
    /// <b>A difference of differences, and the only figure immune to "strong tables win
    /// more".</b> Both arms seat the same four strategies, so anything that acts through the
    /// table's overall strength cancels here and what is left is the direction of the edge.
    /// </remarks>
    public (Measurement WinRate, Measurement TakeRate) Directional(string level)
    {
        var effects = Effects;

        var up = effects.First(effect => effect.Arm == Neighbour.Upstream && effect.Level == level);
        var down = effects.First(effect => effect.Arm == Neighbour.Downstream && effect.Level == level);

        return (
            Measurement.Difference(up.WinRate, down.WinRate),
            Measurement.Difference(up.TakeRate, down.TakeRate));
    }
}
