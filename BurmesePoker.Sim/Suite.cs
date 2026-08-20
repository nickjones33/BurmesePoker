using System.Globalization;
using System.Diagnostics;

using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>What the standing suite is run with.</summary>
public sealed record SuiteOptions
{
    /// <summary>The ladder, weakest first.</summary>
    public IReadOnlyList<Strategy> Strategies { get; init; } = StrategyCatalog.All;

    /// <summary>
    /// The difficulty dial, weakest first (BUILD-PLAN P19).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A second tournament and not more rows in the first one.</b> Every level is the
    /// hardest rung with a handicap, so <c>expert</c> and <c>greedy</c> are the same player
    /// under two names — put in one field they would be a null cell dressed up as a
    /// comparison, and the free-for-all that gives each level its reference win rate would be
    /// diluted by four rungs nobody is offered.
    /// </remarks>
    public IReadOnlyList<Strategy> Levels { get; init; } = StrategyCatalog.Levels;

    /// <summary>Seats at the table (RULES.md §2.1).</summary>
    public int Seats { get; init; } = RoundEngine.MinimumPlayers;

    /// <summary>Games wanted in each cell, rounded up to a whole number of passes.</summary>
    public int GamesPerCell { get; init; } = 2000;

    /// <summary>The one number the whole suite reproduces from.</summary>
    public int MasterSeed { get; init; } = 20260819;

    /// <summary>Turns before a stalled round is given up on.</summary>
    public int TurnCap { get; init; } = 400;

    /// <summary>What the rounds are played for.</summary>
    public Stakes Stakes { get; init; } = Stakes.Standard;

    /// <summary>Whether the games inside a cell run concurrently. Results are identical either way.</summary>
    public bool Parallel { get; init; } = true;
}

/// <summary>
/// One published number, with everything needed to re-examine it.
/// </summary>
/// <param name="Id">A stable key. <b>The documentation quotes this</b>, so it may not be renamed lightly.</param>
/// <param name="Question">What it answers, in a sentence.</param>
/// <param name="Command">The command that produced it (BUILD-PLAN §3.12).</param>
/// <param name="Subject">Who or what it is about.</param>
/// <param name="Metric">What was measured.</param>
/// <param name="Value">The estimate, with its interval.</param>
/// <param name="Verdict">A word, where there is one to say. Empty otherwise.</param>
public sealed record SuiteMeasurement(
    string Id,
    string Question,
    string Command,
    string Subject,
    string Metric,
    Measurement Value,
    string Verdict);

/// <summary>What the suite measured, and the tournaments it mostly came from.</summary>
/// <param name="Options">What it was run with.</param>
/// <param name="Measurements">Every published number.</param>
/// <param name="Tournament">The ladder ranked — the research instrument.</param>
/// <param name="Difficulty">The dial calibrated — the product (BUILD-PLAN P19).</param>
/// <param name="Elapsed">How long it took.</param>
public sealed record SuiteReport(
    SuiteOptions Options,
    IReadOnlyList<SuiteMeasurement> Measurements,
    TournamentReport Tournament,
    TournamentReport Difficulty,
    TimeSpan Elapsed)
{
    /// <summary>
    /// Whether every step of the dial beat the one below it, correction and all.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>A level that is not separated from its neighbour is a lie told to everybody who
    /// reads the menu</b> (BUILD-PLAN §3.12 item 2), so this is checked on every run rather
    /// than at the packet that set the values. It is the same standing as the null test: the
    /// suite exits non-zero if it ever stops holding.
    /// </remarks>
    public bool DialIsSeparated =>
        Difficulty.Verdicts.Count == Difficulty.Options.Strategies.Count - 1
        && Difficulty.Verdicts.All(verdict => verdict.Survives)
        && Difficulty.Pairs.All(cell => cell.Margin.Mean < 0);
}

/// <summary>
/// The standing set of measurements the documentation quotes (BUILD-PLAN P17).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The numbers in the documentation stop being transcribed and start being generated.</b>
/// <c>RULES.md</c> tags every rule with its provenance because a rule without a source cannot be
/// re-examined; a measurement without its origin is worse, because it looks like a fact. Every
/// row here carries the command that made it and the games it came from, and
/// <c>docs/STRATEGY.md</c> quotes the file rather than a session's console.
/// </para>
/// <para>
/// <b>It is deliberately small.</b> A suite that measured everything measurable would never be
/// re-run, and a stale suite is worse than none. What is in it is what the docs actually claim:
/// the ladder at one table, the ladder head to head, the harness's own null test, and P12's
/// headline under both seatings — the one figure this project has already had to correct once.
/// </para>
/// </remarks>
public static class Suite
{
    /// <summary>Where the suite writes unless told otherwise.</summary>
    public const string DefaultPath = "docs/strategy/measurements.csv";

    /// <summary>Plays the standing set.</summary>
    public static SuiteReport Run(SuiteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var clock = Stopwatch.StartNew();

        var tournament = Tournament.Run(new TournamentOptions
        {
            Strategies = options.Strategies,
            Seats = options.Seats,
            GamesPerCell = options.GamesPerCell,
            MasterSeed = options.MasterSeed,
            TurnCap = options.TurnCap,
            Stakes = options.Stakes,
            Parallel = options.Parallel
        });

        // ⚠️ Only the adjacent pairs, because only "n+1 beats n" is a claim the menu makes
        // (BUILD-PLAN P19 acceptance 1b). Correcting a dial over the whole round-robin is a
        // power loss paid for comparisons nobody is making.
        var difficulty = Tournament.Run(new TournamentOptions
        {
            Strategies = options.Levels,
            Pairs = PairFamily.Adjacent,
            Seats = options.Seats,
            GamesPerCell = options.GamesPerCell,
            MasterSeed = options.MasterSeed,
            TurnCap = options.TurnCap,
            Stakes = options.Stakes,
            Parallel = options.Parallel
        });

        var measurements = new List<SuiteMeasurement>();
        var names = string.Join(",", options.Strategies.Select(strategy => strategy.Name));

        var tournamentCommand = string.Create(CultureInfo.InvariantCulture,
            $"BurmesePoker.Sim -- tournament --strategies {names} --seats {options.Seats} "
            + $"--games {options.GamesPerCell} --seed {options.MasterSeed}");

        foreach (var player in tournament.FreeForAll.Players)
        {
            measurements.Add(new SuiteMeasurement(
                $"ladder.free-for-all.{player.Name}",
                "How often does this rung declare with the whole ladder at one table, fully crossed?",
                tournamentCommand,
                player.Name,
                "win rate",
                player.WinRate,
                string.Empty));
        }

        for (var index = 0; index < tournament.Pairs.Count; index++)
        {
            var cell = tournament.Pairs[index];
            var verdict = tournament.Verdicts[index];

            measurements.Add(new SuiteMeasurement(
                $"ladder.head-to-head.{cell.Row}-over-{cell.Column}",
                "Head to head at every seating in which both are at the table, how many points "
                + "of win rate separate them?",
                tournamentCommand,
                cell.Label,
                "win rate margin",
                cell.Margin,
                verdict.Survives ? "separated (Holm)" : verdict.Separated ? "raw only" : "inside the interval"));
        }

        foreach (var standing in tournament.Ranking)
        {
            measurements.Add(new SuiteMeasurement(
                $"ladder.rank.{standing.Name}",
                "Averaged over the field, how many points does this rung beat an opponent by?",
                tournamentCommand,
                standing.Name,
                "mean head-to-head margin",
                new Measurement(tournament.Comparisons, standing.MeanMargin, 0),
                $"{standing.Beaten}-{standing.LostTo}-{standing.Undecided} beaten/lost/undecided"));
        }

        foreach (var player in tournament.NullCell.Players)
        {
            measurements.Add(new SuiteMeasurement(
                $"harness.null-test.{player.Name}",
                $"A strategy against a copy of itself must win {1.0 / options.Seats:0.0%} of the "
                + "games. Anything else is the apparatus, not the strategy.",
                tournamentCommand,
                player.Name,
                "win rate",
                player.WinRate,
                tournament.NullTestHolds ? "holds" : "FAILED"));
        }

        foreach (var check in tournament.Pairing)
        {
            measurements.Add(new SuiteMeasurement(
                $"pairing.{check.Scope}.{check.Label}",
                "Paired standard error over independent, on the same data. Below one is variance "
                + "reduction; above one means the independent formula was understating it.",
                tournamentCommand,
                check.Label,
                "standard error ratio",
                new Measurement(check.Paired.Count, check.Ratio, 0),
                check.Ratio < 1 ? "pairing narrows" : "pairing widens"));
        }

        foreach (var (plan, balanced) in new[] { ("rotate", false), ("balanced", true) })
        {
            var headline = Headline(options, balanced);

            var command = string.Create(CultureInfo.InvariantCulture,
                $"BurmesePoker.Sim -- --strategies greedy,simple --seating {plan} --seats {options.Seats} "
                + $"--games {headline.Games} --seed {options.MasterSeed}");

            foreach (var series in headline.Series)
            {
                measurements.Add(new SuiteMeasurement(
                    $"headline.{plan}.{series.Name}",
                    "P12's headline: greedy against simple at four seats. ⚠️ The rotation seats "
                    + "[g,s,g,s], in which every greedy sits downstream of a simple — the honest "
                    + "strategy-vs-strategy figure is the balanced one (P16).",
                    command,
                    series.Name,
                    "win rate",
                    Measurement.Of(series.WinRate),
                    string.Empty));
            }
        }

        var levels = string.Join(",", options.Levels.Select(level => level.Name));

        var difficultyCommand = string.Create(CultureInfo.InvariantCulture,
            $"BurmesePoker.Sim -- tournament --strategies {levels} --pairs adjacent "
            + $"--seats {options.Seats} --games {options.GamesPerCell} --seed {options.MasterSeed}");

        foreach (var player in difficulty.FreeForAll.Players)
        {
            measurements.Add(new SuiteMeasurement(
                $"difficulty.reference-table.{player.Name}",
                "The reference table: every difficulty level at one table, fully crossed. This is "
                + "the ordering the menu claims, and it is what a person actually sits down to.",
                difficultyCommand,
                player.Name,
                "win rate",
                player.WinRate,
                string.Empty));
        }

        for (var index = 0; index < difficulty.Pairs.Count; index++)
        {
            var cell = difficulty.Pairs[index];
            var verdict = difficulty.Verdicts[index];

            measurements.Add(new SuiteMeasurement(
                $"difficulty.step.{cell.Column}-over-{cell.Row}",
                "One step of the dial, head to head. ⚠️ The family is the k−1 adjacent steps and "
                + "not the round-robin, because 'n+1 beats n' is the only claim a monotone dial "
                + "makes (BUILD-PLAN P19).",
                difficultyCommand,
                $"{cell.Column} over {cell.Row}",
                "win rate margin",
                cell.Margin with { Mean = -cell.Margin.Mean },
                verdict.Survives ? "separated (Holm)" : verdict.Separated ? "raw only" : "inside the interval"));
        }

        foreach (var level in options.Levels)
        {
            measurements.Add(new SuiteMeasurement(
                $"difficulty.mistake-rate.{level.Name}",
                "ε — how often this level throws the second-best card off its own ranking rather "
                + "than the best. Placed so the levels are evenly spaced in results, which is not "
                + "evenly spaced in ε (BUILD-PLAN P19).",
                difficultyCommand,
                level.Name,
                "mistake rate",
                new Measurement(0, Rate(level.Name), 0),
                string.Empty));
        }

        clock.Stop();

        return new SuiteReport(options, measurements, tournament, difficulty, clock.Elapsed);
    }

    /// <summary>
    /// What ε a level carries, out of the one list there is.
    /// </summary>
    /// <remarks>
    /// A calibration probe answers too, because a sweep is a legitimate thing to run the suite
    /// over — and a name that is neither is not a level, so it carries no rate.
    /// </remarks>
    private static double Rate(string level) => DifficultyLadder.FindOrProbe(level)?.MistakeRate ?? 0;

    /// <summary>P12's headline run, under one seating plan or the other.</summary>
    private static (int Games, IReadOnlyList<StrategySeries> Series) Headline(SuiteOptions options, bool balanced)
    {
        IReadOnlyList<Strategy> pair =
        [
            StrategyCatalog.Resolve("greedy"),
            StrategyCatalog.Resolve("simple")
        ];

        var assignments = balanced ? SeatingPlan.Balanced(pair, options.Seats) : null;
        var passes = assignments?.Count ?? pair.Count;
        var games = (options.GamesPerCell + passes - 1) / passes * passes;

        var report = Simulator.Run(new SimulationOptions
        {
            Strategies = pair,
            Assignments = assignments,
            Seats = options.Seats,
            Games = games,
            RoundsPerGame = 1,
            MasterSeed = options.MasterSeed,
            Stakes = options.Stakes,
            TurnCap = options.TurnCap,
            Parallel = options.Parallel
        }.Validated());

        return (games, StrategySeries.Of(report));
    }
}

/// <summary>The suite as a file (BUILD-PLAN §3.12: a number carries the command that made it).</summary>
public static class SuiteCsv
{
    /// <summary>Every measurement, one to a row.</summary>
    public static IEnumerable<string> Rows(SuiteReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        yield return "id,subject,metric,games,mean,error,interval,verdict,seed,seats,question,command";

        foreach (var measurement in report.Measurements)
        {
            // ⚠️ An error of "0.000000" reads as a zero-width interval, which is a much
            // stronger claim than "this statistic does not carry one". A mean margin over the
            // field and a standard-error ratio are both derived from figures that have
            // intervals and have none of their own, so their two columns are left empty.
            var error = measurement.Value.StandardError > 0 ? Number(measurement.Value.StandardError) : string.Empty;
            var interval = measurement.Value.StandardError > 0 ? Number(measurement.Value.Interval) : string.Empty;

            yield return string.Create(CultureInfo.InvariantCulture,
                $"{Quote(measurement.Id)},{Quote(measurement.Subject)},{measurement.Metric},"
                + $"{measurement.Value.Count},{Number(measurement.Value.Mean)},"
                + $"{error},{interval},"
                + $"{Quote(measurement.Verdict)},{report.Options.MasterSeed},{report.Options.Seats},"
                + $"{Quote(measurement.Question)},{Quote(measurement.Command)}");
        }
    }

    /// <summary>Writes them, making the directory if it is not there.</summary>
    public static void WriteTo(string path, SuiteReport report)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, Rows(report));
    }

    private static string Quote(string text) =>
        text.Contains(',', StringComparison.Ordinal) || text.Contains('"', StringComparison.Ordinal)
            ? $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : text;

    private static string Number(double value) =>
        double.IsFinite(value)
            ? value.ToString("0.000000", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
