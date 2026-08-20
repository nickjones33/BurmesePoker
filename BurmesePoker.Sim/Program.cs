using System.Diagnostics;
using System.Globalization;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;

// Batch play, seeded and parallel (BUILD-PLAN P12). Domain only: this project cannot print a
// game any more than it can play one interactively — Spectre lives in BurmesePoker.Console.

try
{
    if (args.Length > 0 && args[0] == "bench")
    {
        return Bench(args[1..]);
    }

    if (args.Length > 0 && args[0] == "neighbours")
    {
        return Neighbours(args[1..]);
    }

    if (args.Length > 0 && args[0] == "tournament")
    {
        return RunTournament(args[1..]);
    }

    if (args.Length > 0 && args[0] == "suite")
    {
        return RunSuite(args[1..]);
    }

    return args.Length > 0 && args[0] == "replay" ? Replay(args[1..]) : Run(args);
}
catch (ArgumentException problem)
{
    Console.Error.WriteLine(problem.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage());
    return 1;
}
catch (Exception problem) when (problem is JournalException or IOException or UnauthorizedAccessException)
{
    // A journal that cannot be read is a bad file, not a crash — and it says which line
    // or which decision it gave up at (BUILD-PLAN P14).
    Console.Error.WriteLine(problem.Message);
    return 1;
}

static int Run(string[] args)
{
    var arguments = Arguments.Parse(args);

    if (arguments.Flag("help") || arguments.Flag("h"))
    {
        Console.WriteLine(Usage());
        return 0;
    }

    var strategies = Ladder(arguments.Value("strategies", "greedy,simple"));
    var seats = arguments.Number("seats", 4);
    var balanced = arguments.Value("seating", "rotate").Equals("balanced", StringComparison.OrdinalIgnoreCase);

    var options = new SimulationOptions
    {
        Strategies = strategies,

        // ⚠️ Balanced seating is what makes a per-strategy figure a strategy figure
        // (BUILD-PLAN P16). The rotation seats one pattern, so every A is fed by a B —
        // the honest answer to "what happens at that table" and not to "which plays better".
        Assignments = balanced ? SeatingPlan.Balanced(strategies, seats) : null,
        Seats = seats,
        Games = arguments.Number("games", 200),
        RoundsPerGame = arguments.Number("rounds", 1),
        MasterSeed = arguments.Number("seed", 20260818),
        TurnCap = arguments.Number("turn-cap", 400),
        Journal = arguments.Has("journal") ? Fidelity(arguments.Value("fidelity", "thin")) : null,
        Parallel = !arguments.Flag("serial"),
        MaxDegreeOfParallelism = arguments.Has("threads") ? arguments.Number("threads", 0) : null
    }.Validated();

    Console.WriteLine(
        $"{options.Games} games x {options.RoundsPerGame} round(s), {options.Seats} seats, "
        + $"seed {options.MasterSeed}, {(options.Parallel ? "parallel" : "serial")}, "
        + $"{(balanced ? $"{options.Assignments!.Count} seatings balanced" : "seating rotated")}, "
        + $"strategies {string.Join(" vs ", options.Strategies.Select(strategy => strategy.Name))}");

    var report = Simulator.Run(options);

    Report(report);

    Write(arguments, report);
    return 0;
}

// Reading a game back is playing it with different seats (BUILD-PLAN P14), so a replayed run
// summarises through the same code a played one does — which is what makes "identical" a
// diff rather than an impression.
static int Replay(string[] args)
{
    var path = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal)
        ? args[0]
        : throw new ArgumentException("replay wants a journal to read, e.g. replay run.jsonl.");

    var arguments = Arguments.Parse(args[1..]);
    var journals = JournalReport.ReadFrom(path);

    Console.WriteLine(
        $"{journals.Count} journal(s) from {path}, "
        + $"{journals.Sum(journal => journal.Header.Rounds)} settled round(s), "
        + $"rules rev {journals[0].Header.RulesRevision}, {journals[0].Header.Fidelity.ToString().ToLowerInvariant()}");

    var report = BurmesePoker.Sim.Replay.Run(journals);

    Report(report);
    Write(arguments, report);
    return 0;
}

// The experiment BUILD-PLAN P16 asks for: a focal seat, a dial in the seat before it, and the
// same dial in the seat after it as the control. Both arms seat the same four strategies, so
// what separates them is only which way the discards flow.
static int Neighbours(string[] args)
{
    var arguments = Arguments.Parse(args);

    var options = new NeighbourOptions
    {
        Focal = StrategyCatalog.Resolve(arguments.Value("focal", "greedy")),
        Levels = Ladder(arguments.Value("levels", "random,simple,greedy,cautious")),
        Filler = StrategyCatalog.Resolve(arguments.Value("filler", "simple")),
        Reference = StrategyCatalog.Resolve(arguments.Value("reference", "greedy")),
        Seats = arguments.Number("seats", 4),
        GamesPerCell = arguments.Number("games", 2000),
        MasterSeed = arguments.Number("seed", 20260819),
        TurnCap = arguments.Number("turn-cap", 400),
        Parallel = !arguments.Flag("serial")
    }.Validated();

    Console.WriteLine(
        $"focal {options.Focal.Name} at {options.Seats} seats, filler {options.Filler.Name}, "
        + $"levels {string.Join(", ", options.Levels.Select(level => level.Name))}, "
        + $"{options.GamesPerCell} games x {options.Levels.Count * 2} cells, seed {options.MasterSeed}");
    Console.WriteLine(
        "reproduce with: BurmesePoker.Sim -- neighbours "
        + $"--focal {options.Focal.Name} --levels {string.Join(",", options.Levels.Select(level => level.Name))} "
        + $"--filler {options.Filler.Name} --reference {options.Reference.Name} "
        + $"--seats {options.Seats} --games {options.GamesPerCell} --seed {options.MasterSeed}");

    var report = NeighbourExperiment.Run(options);

    ReportNeighbours(report);

    if (arguments.Has("csv"))
    {
        var path = arguments.Value("csv", "neighbours.csv");
        NeighbourCsv.WriteTo(path, report);
        Console.WriteLine($"Cells and effects written to {path}");
    }

    return 0;
}

static void ReportNeighbours(NeighbourReport report)
{
    var focal = report.Options.Focal.Name;

    Console.WriteLine();
    Console.WriteLine(
        $"{report.Cells.Sum(cell => cell.Settled)} settled games in {report.Elapsed.TotalSeconds:0.0} s, "
        + $"{report.Cells.Sum(cell => cell.Abandoned)} abandoned");
    Console.WriteLine();

    Console.WriteLine($"{"arm",-11}{"level",-10}{"games",7}{"seat balance",20}{"win % of " + focal,20}"
        + $"{"take %",18}{"$/round",16}{"level win %",13}");

    foreach (var cell in report.Cells)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{cell.Arm.ToString().ToLowerInvariant(),-11}{cell.Level,-10}{cell.Settled,7}"
            + $"{string.Join('/', cell.FocalSeatGames),20}"
            + $"{cell.WinRate.Mean * 100,13:0.0} ± {cell.WinRate.Interval * 100,4:0.0}"
            + $"{cell.TakeRate.Mean * 100,11:0.0} ± {cell.TakeRate.Interval * 100,4:0.0}"
            + $"{cell.NetPerRound.Mean,9:0.00} ± {cell.NetPerRound.Interval,4:0.00}"
            + $"{cell.LevelWinRate.Mean * 100,13:0.0}"));
    }

    Console.WriteLine();
    Console.WriteLine($"Effects on {focal}, against {report.Options.Reference.Name} in the same seat "
        + "(95% intervals, points):");
    Console.WriteLine();
    Console.WriteLine($"{"arm",-11}{"level",-10}{"win %",20}{"take %",20}{"$/round",18}");

    foreach (var effect in report.Effects)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{effect.Arm.ToString().ToLowerInvariant(),-11}{effect.Level,-10}"
            + $"{Signed(effect.WinRate.Mean * 100, "0.0"),13} ± {effect.WinRate.Interval * 100,4:0.0}"
            + $"{Signed(effect.TakeRate.Mean * 100, "0.0"),13} ± {effect.TakeRate.Interval * 100,4:0.0}"
            + $"{Signed(effect.NetPerRound.Mean, "0.00"),11} ± {effect.NetPerRound.Interval,4:0.00}"));
    }

    Console.WriteLine();
    Console.WriteLine("Upstream less downstream — the edge itself, with the table's strength cancelled:");
    Console.WriteLine();
    Console.WriteLine($"{"level",-10}{"win %",20}{"take %",20}  {"verdict",-10}");

    foreach (var level in report.Options.Levels.Where(level => level.Name != report.Options.Reference.Name))
    {
        var (winRate, takeRate) = report.Directional(level.Name);

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{level.Name,-10}"
            + $"{Signed(winRate.Mean * 100, "0.0"),13} ± {winRate.Interval * 100,4:0.0}"
            + $"{Signed(takeRate.Mean * 100, "0.0"),13} ± {takeRate.Interval * 100,4:0.0}"
            + $"  {(winRate.IsSeparatedFromZero ? "separated" : "inside the interval")}"));
    }
}

// The round-robin BUILD-PLAN P17 asks for: every unordered pair head to head, the free-for-all
// beside it because the two answer different questions (P16), a null cell in which the harness
// measures itself, and a family-wise correction because k(k-1)/2 comparisons at a 95% interval
// is a machine for promoting noise to a rung.
static int RunTournament(string[] args)
{
    var arguments = Arguments.Parse(args);
    var strategies = Ladder(arguments.Value("strategies", WholeLadder()));

    var options = new TournamentOptions
    {
        Strategies = strategies,
        Seats = arguments.Number("seats", 4),
        GamesPerCell = arguments.Number("games", 2000),
        MasterSeed = arguments.Number("seed", 20260819),
        TurnCap = arguments.Number("turn-cap", 400),
        Parallel = !arguments.Flag("serial"),
        NullTest = arguments.Has("null") ? arguments.Value("null", "") : null,
        // ⚠️ The family is the claim, not the cells that could exist (BUILD-PLAN P19): a dial
        // claims only that each level beats the one below it, so correcting a dial over the
        // whole round-robin throws power away for comparisons nobody is making.
        Pairs = arguments.Value("pairs", "all").Equals("adjacent", StringComparison.OrdinalIgnoreCase)
            ? PairFamily.Adjacent
            : PairFamily.RoundRobin
    }.Validated();

    var names = string.Join(",", strategies.Select(strategy => strategy.Name));

    Console.WriteLine(
        $"tournament: {string.Join(", ", strategies.Select(strategy => strategy.Name))} at "
        + $"{options.Seats} seats, "
        + $"{(options.Pairs == PairFamily.Adjacent ? strategies.Count - 1 : strategies.Count * (strategies.Count - 1) / 2)}"
        + $" pair(s) + free-for-all + null, {options.GamesPerCell} games a cell, seed {options.MasterSeed}");
    Console.WriteLine(
        $"reproduce with: BurmesePoker.Sim -- tournament --strategies {names} "
        + $"{(options.Pairs == PairFamily.Adjacent ? "--pairs adjacent " : string.Empty)}"
        + $"--seats {options.Seats} --games {options.GamesPerCell} --seed {options.MasterSeed}");

    var report = Tournament.Run(options);

    ReportTournament(report);

    if (arguments.Has("csv"))
    {
        var path = arguments.Value("csv", "tournament.csv");
        TournamentCsv.WriteTo(path, report);
        Console.WriteLine($"Cells, comparisons and the ranking written to {path}");
    }

    return 0;
}

static void ReportTournament(TournamentReport report)
{
    var field = report.Options.Strategies;

    Console.WriteLine();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{report.Games} games in {report.Elapsed.TotalSeconds:0.0} s, "
        + $"{report.Pairs.Sum(cell => cell.Abandoned) + report.FreeForAll.Abandoned + report.NullCell.Abandoned}"
        + $" abandoned at the turn cap"));

    Console.WriteLine();
    Console.WriteLine("Head to head — the row's win rate less the column's, game by game, in points.");
    Console.WriteLine("A star is a margin that survived the family-wise correction below.");
    Console.WriteLine();
    Console.Write($"{"",-12}");

    foreach (var column in field)
    {
        Console.Write($"{column.Name,16}");
    }

    Console.WriteLine();

    foreach (var row in field)
    {
        Console.Write($"{row.Name,-12}");

        foreach (var column in field)
        {
            if (row.Name == column.Name)
            {
                Console.Write($"{"·",16}");
                continue;
            }

            // ⚠️ Under --pairs adjacent most of the matrix is empty, and an empty cell is a
            // pair that was never played rather than a dead heat (BUILD-PLAN P19).
            if (!report.Met(row.Name, column.Name))
            {
                Console.Write($"{"",16}");
                continue;
            }

            var (margin, verdict) = report.Head(row.Name, column.Name);

            // A star marks a margin that survived the correction, so the matrix and the
            // verdict table cannot disagree with one another.
            var cell = string.Create(CultureInfo.InvariantCulture,
                $"{Signed(margin.Mean * 100, "0.0")} ± {margin.Interval * 100:0.0}{(verdict.Survives ? "*" : "")}");

            Console.Write($"{cell,16}");
        }

        Console.WriteLine();
    }

    Console.WriteLine();
    Console.WriteLine("Ranking, by mean head-to-head margin over the field:");
    Console.WriteLine();
    Console.WriteLine($"{"#",-3}{"strategy",-12}{"mean margin",14}{"free-for-all win %",22}{"beat/lost/undecided",22}");

    var place = 0;

    foreach (var standing in report.Ranking)
    {
        place++;

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{place,-3}{standing.Name,-12}{Signed(standing.MeanMargin * 100, "0.0"),14}"
            + $"{standing.FreeForAllWinRate.Mean * 100,15:0.0} ± {standing.FreeForAllWinRate.Interval * 100,4:0.0}"
            + $"{standing.Beaten + "/" + standing.LostTo + "/" + standing.Undecided,22}"));
    }

    Console.WriteLine();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{report.Comparisons} comparison(s) in the family. Holm-Bonferroni at alpha "
        + $"{report.Options.Alpha:0.###}: a raw \"separated\" that does not survive is not a finding."));
    Console.WriteLine();
    Console.WriteLine($"{"comparison",-26}{"margin",16}{"p",13}{"threshold",13}  {"raw",-12}{"corrected",-18}");

    foreach (var verdict in report.Verdicts.OrderBy(v => v.Rank))
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{verdict.Label,-26}{Signed(verdict.Difference.Mean * 100, "0.0"),9} ± {verdict.Difference.Interval * 100,4:0.0}"
            + $"{verdict.PValue,13:0.00e+00}{verdict.Threshold,13:0.00000}  "
            + $"{(verdict.Separated ? "separated" : "inside"),-12}{(verdict.Survives ? "survives" : "does not survive"),-18}"));
    }

    Console.WriteLine();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"Null test — {report.NullCell.Row} against a copy of itself. A fair table gives each "
        + $"{1.0 / report.Options.Seats * 100:0.0}%:"));
    Console.WriteLine();

    foreach (var player in report.NullCell.Players)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{player.Name,-24}{player.WinRate.Mean * 100,9:0.0} ± {player.WinRate.Interval * 100,4:0.0}"
            + $"   seats {string.Join('/', player.SeatRounds)}"));
    }

    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{"margin",-24}{Signed(report.NullCell.Margin.Mean * 100, "0.0"),9} ± "
        + $"{report.NullCell.Margin.Interval * 100,4:0.0}   "
        + $"{(report.NullTestHolds ? "the harness finds no difference where there is none" : "⚠️ THE HARNESS IS BIASED")}"));

    Console.WriteLine();
    Console.WriteLine("Pairing — the per-game difference against the add-the-variances formula, same data:");
    Console.WriteLine();
    Console.WriteLine($"{"scope",-14}{"comparison",-40}{"paired se",12}{"independent",13}{"ratio",8}");

    foreach (var check in report.Pairing)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{check.Scope,-14}{check.Label,-40}{check.Paired.StandardError * 100,12:0.000}"
            + $"{check.Independent.StandardError * 100,13:0.000}{check.Ratio,8:0.00}"));
    }

    Console.WriteLine();
    Console.WriteLine(
        "⚠️ Within a cell pairing WIDENS the interval and that is correct: exactly one seat "
        + "declares, so two strategies at one table are negatively correlated and the "
        + "independent formula understates the margin. Across cells it narrows, which is the "
        + "variance reduction common random numbers were for.");
}

// The standing set of numbers the documentation quotes, generated rather than transcribed
// (BUILD-PLAN §3.12, P17).
static int RunSuite(string[] args)
{
    var arguments = Arguments.Parse(args);

    var options = new SuiteOptions
    {
        Strategies = Ladder(arguments.Value("strategies", WholeLadder())),
        Seats = arguments.Number("seats", 4),
        GamesPerCell = arguments.Number("games", 2000),
        MasterSeed = arguments.Number("seed", 20260819),
        TurnCap = arguments.Number("turn-cap", 400),
        Parallel = !arguments.Flag("serial")
    };

    var path = arguments.Value("out", Suite.DefaultPath);

    Console.WriteLine(
        $"suite: {options.GamesPerCell} games a cell, {options.Seats} seats, seed {options.MasterSeed} "
        + $"-> {path}");

    var report = Suite.Run(options);

    SuiteCsv.WriteTo(path, report);

    Console.WriteLine();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{report.Measurements.Count} measurement(s) in {report.Elapsed.TotalSeconds:0.0} s, written to {path}"));
    Console.WriteLine();
    Console.WriteLine($"{"id",-46}{"subject",-24}{"value",18}{"verdict",-24}");

    foreach (var measurement in report.Measurements)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{measurement.Id,-46}{measurement.Subject,-24}"
            + $"{measurement.Value.Mean,11:0.0000} ± {measurement.Value.Interval,5:0.000}"
            + $"  {measurement.Verdict,-24}"));
    }

    if (!report.Tournament.NullTestHolds)
    {
        Console.Error.WriteLine("⚠️ The null test failed: the harness measures a difference where there is none.");
        return 1;
    }

    // 🔥 A level that is not separated from the one below it is a lie told to everybody who
    // reads the menu (BUILD-PLAN §3.12 item 2), so the dial is checked here rather than only
    // in the packet that calibrated it — a new rung raises the ceiling and moves every level.
    if (!report.DialIsSeparated)
    {
        Console.Error.WriteLine(
            "⚠️ The difficulty dial is not monotone: some level is not separated from the one below it. "
            + "Re-space the mistake rates, or delete the level (BUILD-PLAN §3.12 item 2).");
        return 1;
    }

    return 0;
}

// ⚠️ The default field is the catalog rather than a list written out here (BUILD-PLAN P18,
// P20). Four of these names were spelled out in three places until P20 added a fifth rung and
// none of them saw it — which is the same defect P18 fixed one layer down, in a front end
// nobody thinks of as one.
static string WholeLadder() => string.Join(",", StrategyCatalog.All.Select(strategy => strategy.Name));

static IReadOnlyList<Strategy> Ladder(string names) =>
[
    .. names
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(StrategyCatalog.Resolve)
];

static void Write(Arguments arguments, SimulationReport report)
{
    if (arguments.Has("csv"))
    {
        var path = arguments.Value("csv", "sim.csv");
        CsvReport.WriteTo(path, report);
        Console.WriteLine($"Rows written to {path}");
    }

    if (arguments.Has("journal") && report.Games.Any(game => game.Journal is not null))
    {
        var path = arguments.Value("journal", "run.jsonl");
        JournalReport.WriteTo(path, report);
        Console.WriteLine($"Journals written to {path}");
    }
}

static JournalFidelity Fidelity(string name) => name.ToLowerInvariant() switch
{
    "thin" => JournalFidelity.Thin,
    "rich" => JournalFidelity.Rich,
    _ => throw new ArgumentException($"--fidelity is thin or rich, not '{name}'.")
};

// ⚠️ Every figure carries an interval (BUILD-PLAN P17). Two win rates a reader will rank —
// 36.1% and 36.8% — took 32,000 games to establish were the same number (P15), and a table of
// point estimates invites exactly that ranking. The unit of independence is the game, never
// the seat or the turn: see Measurement and StrategySeries.
static void Report(SimulationReport report)
{
    Console.WriteLine();
    Console.WriteLine(
        $"{report.Rounds} rounds in {report.Elapsed.TotalSeconds:0.00} s "
        + $"({report.RoundsPerSecond:0} rounds/s), {report.TurnsPerRound:0.0} turns a round, "
        + $"{report.Reshuffles} reshuffle(s), {report.AbandonedGames} game(s) abandoned at the turn cap");
    Console.WriteLine();

    var series = StrategySeries.Of(report).ToDictionary(one => one.Name, StringComparer.Ordinal);

    Console.WriteLine(
        $"{"strategy",-10}{"rounds",8}{"wins",7}{"win %",14}{"$/round",15}{"side $/r",15}"
        + $"{"turns",14}{"covered",13}{"take %",14}{"claim %",14}   95% intervals, over games");

    foreach (var strategy in report.Strategies)
    {
        var one = series[strategy.Name];

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{strategy.Name,-10}{strategy.Rounds,8}{strategy.Wins,7}"
            + $"{Percent(one.WinRate),14}{Money(one.NetPerRound),15}{Money(one.SideBetPerRound),15}"
            + $"{Count(one.TurnsToWin),14}{Count(one.CoveredWhenLosing),13}"
            + $"{Percent(one.TakeRate),14}{Percent(one.ClaimRate),14}"));
    }

    Console.WriteLine();
    Console.WriteLine($"{report.Games.Count} game(s) are the trials; a seat is not one.");
}

static string Percent(IReadOnlyList<GameValue> values) => Show(Measurement.Of(values), 100, "0.0");

static string Money(IReadOnlyList<GameValue> values) => Show(Measurement.Of(values), 1, "0.00");

static string Count(IReadOnlyList<GameValue> values) => Show(Measurement.Of(values), 1, "0.0");

// ⚠️ A two-section format picks its section from the *rounded* value, so -0.04 under
// "+0.0;-0.0" prints as "-+0.0": the positive section supplies the plus and the runtime
// prepends the minus. Every signed figure here is built by hand instead.
static string Signed(double value, string format) =>
    string.Create(CultureInfo.InvariantCulture,
        $"{(value < 0 ? "-" : "+")}{Math.Abs(value).ToString(format, CultureInfo.InvariantCulture)}");

static string Show(Measurement measurement, double scale, string format) =>
    measurement.Count == 0
        ? "-"
        : string.Create(CultureInfo.InvariantCulture,
            $"{(measurement.Mean * scale).ToString(format, CultureInfo.InvariantCulture)} "
            + $"± {(measurement.Interval * scale).ToString(format, CultureInfo.InvariantCulture)}");

// The measurement pass P12 asks for before anything is optimised (BUILD-PLAN §3.7 item 4):
// the bot's inner loop is the partial cover, and the engine's is the exact-cover evaluator.
static int Bench(string[] args)
{
    var arguments = Arguments.Parse(args);
    var hands = arguments.Number("hands", 2000);
    var random = new Random(arguments.Number("seed", 20260818));

    var dealt = new List<Card[]>(hands);

    for (var hand = 0; hand < hands; hand++)
    {
        var deck = Deck.TwoDecks();
        deck.Shuffle(random);
        dealt.Add([.. deck.Cards.Take(13)]);
    }

    Console.WriteLine($"{hands} random thirteen-card hands");
    Console.WriteLine($"PartialCover.Best          {Time(dealt, hand => PartialCover.Best(hand).CoveredCount != -1)}");
    Console.WriteLine($"HandEvaluator.TryFindCover {Time(dealt, hand => HandEvaluator.TryFindCover(hand, out _))}");
    // ⚠️ One better than the hand can actually do, which is the question the outs rung asks
    // hundreds of times a turn and the expensive half of it: the answer is "no", so the search
    // cannot stop early and has to be pruned to the end (BUILD-PLAN P21).
    var bars = dealt.ToDictionary(hand => hand, hand => PartialCover.Best(hand).CoveredCount + 1);
    Console.WriteLine($"PartialCover.CoversAtLeast {Time(dealt, hand => PartialCover.CoversAtLeast(hand, bars[hand]))}");

    // 🔥 The primitives are no longer the whole story (BUILD-PLAN P21 acceptance 2). A rung
    // that asks a partial cover per value per candidate costs two orders of magnitude more
    // than the search underneath it, so what has to be timed is the decision — and the budget
    // the packet set itself is stated against greedy's rounds a second, not against microseconds.
    var rounds = arguments.Number("rounds", 200);
    var rungs = arguments.Value("strategies", string.Join(",", BotCatalog.All.Select(rung => rung.Name)));

    Console.WriteLine();
    Console.WriteLine($"{rounds} rounds, 4 seats, one rung at every seat, serial, seed {arguments.Number("seed", 20260818)}");

    var reference = 0.0;

    foreach (var name in rungs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        // ⚠️ Warmed first, and it is not a nicety: without it the rung measured first carries
        // everything's jitting and comes out slower than the rung above it, which is how the
        // first run of this said cautious was faster than greedy.
        Simulator.Run(Table(name, Math.Max(1, rounds / 20), arguments).Validated());

        var played = Simulator.Run(Table(name, rounds, arguments).Validated());

        // Greedy is the reference because the budget is: the outs rung may not cost more than
        // ten times a greedy round (BUILD-PLAN P21).
        if (name.Equals(BotCatalog.Resolve("greedy").Name, StringComparison.OrdinalIgnoreCase))
        {
            reference = played.RoundsPerSecond;
        }

        var perTurn = played.Turns == 0 ? 0 : played.Elapsed.TotalMilliseconds * 1000 / played.Turns;

        var relative = reference > 0
            ? string.Create(CultureInfo.InvariantCulture, $"  {reference / played.RoundsPerSecond,6:0.0}x greedy")
            : string.Empty;

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{name,-10}{played.RoundsPerSecond,8:0.0} rounds/s  {perTurn,8:0} us/turn  {played.TurnsPerRound,5:0.0} turns/round{relative}"));
    }

    return 0;
}

// One rung at all four seats, played serially so that a per-turn figure is a per-turn figure.
static SimulationOptions Table(string rung, int rounds, Arguments arguments) => new()
{
    Strategies = [StrategyCatalog.Resolve(rung)],
    Seats = 4,
    Games = rounds,
    RoundsPerGame = 1,
    MasterSeed = arguments.Number("seed", 20260818),
    TurnCap = arguments.Number("turn-cap", 400),
    Parallel = false
};

static string Time(IReadOnlyList<Card[]> hands, Func<Card[], bool> work)
{
    foreach (var hand in hands.Take(50))
    {
        work(hand);
    }

    var clock = Stopwatch.StartNew();

    foreach (var hand in hands)
    {
        work(hand);
    }

    clock.Stop();

    return string.Create(CultureInfo.InvariantCulture,
        $"{clock.Elapsed.TotalMilliseconds * 1000 / hands.Count,8:0} us/hand "
        + $"({hands.Count / clock.Elapsed.TotalSeconds,9:0} hands/s)");
}

// ⚠️ The ladder is read from the catalog and never typed out (BUILD-PLAN P20): the last two
// rungs were added to a list that three other places also kept, and the help was one of them.
static string Usage() => $"""
    BurmesePoker.Sim — batch play, seeded and parallel.

      dotnet run --project BurmesePoker.Sim -- [options]
      dotnet run --project BurmesePoker.Sim -- bench [--hands N] [--rounds N] [--strategies a,b] [--seed N]
      dotnet run --project BurmesePoker.Sim -- replay PATH [--csv PATH]
      dotnet run --project BurmesePoker.Sim -- neighbours [options]
      dotnet run --project BurmesePoker.Sim -- tournament [options]
      dotnet run --project BurmesePoker.Sim -- suite [options]

      --strategies a,b   who is playing, rotated through the seats  (greedy,simple)
                         the ladder, weakest first: {WholeLadder()}
                         the difficulty dial: {string.Join(", ", StrategyCatalog.Levels.Select(level => level.Name))}
                         a calibration probe: greedy@0.35 — a rung with a mistake rate
      --seating S        rotate one pattern, or play every balanced
                         assignment of the strategies across the seats
                         (rotate | balanced)                        (rotate)
      --seats N          4 to 6                                     (4)
      --games N          independent games                          (200)
      --rounds N         rounds per game, banks carrying over       (1)
      --seed N           master seed; every game derives from it    (20260818)
      --turn-cap N       turns before a stalled round is given up   (400)
      --serial           play the games one at a time
      --threads N        cap the concurrency
      --csv PATH         write a row per seat per round
      --journal PATH     write every decision every seat made, as JSON Lines
      --fidelity F       how much of each decision to journal: thin, rich (thin)

    neighbours — does the player before you decide your game? A focal seat with the
    ladder in the seat before it, and the same ladder in the seat after it as a control.

      --focal NAME       the seat being measured                    (greedy)
      --levels a,b,c     what goes beside it       (random,simple,greedy,cautious)
                         — P16's arm, written out rather than the whole ladder
      --filler NAME      the seats that are neither                 (simple)
      --reference NAME   the level the others are reported against  (greedy)
      --games N          games in each of the 2 x |levels| cells    (2000)
      --seats N, --seed N, --turn-cap N, --serial, --csv PATH  as above

    tournament — rank every player against every other, with an interval you can trust.
    Every unordered pair plays a head-to-head cell at every seating in which both are at
    the table; the free-for-all is reported beside it because the two answer different
    questions; one cell plays a strategy against a copy of itself, which is the harness
    measuring its own bias. k(k-1)/2 comparisons are Holm-corrected, and a raw "separated"
    that does not survive the correction is shown as not surviving it.

      --strategies a,b,c the field                       (the whole ladder)
      --pairs P          every unordered pair, or only each against the next
                         one named — which is all a monotone dial claims
                         (all | adjacent)                             (all)
      --games N          games a cell, rounded up to a whole number of seatings  (2000)
      --null NAME        who plays the null cell          (the last one named)
      --seats N, --seed N, --turn-cap N, --serial, --csv PATH  as above

    suite — play the standing set of measurements the documentation quotes and write them
    out. A published number carries the command that made it (BUILD-PLAN §3.12), so
    docs/STRATEGY.md quotes this file rather than a session's console. It ranks the skill
    ladder, calibrates the difficulty dial over its adjacent steps, and exits non-zero if
    the null test fails or the dial stops being monotone.

      --out PATH         where to write         (docs/strategy/measurements.csv)
      --strategies, --games, --seats, --seed, --turn-cap, --serial  as above

    A journal replays against any build; a seed only replays against the one that wrote it.
    """;

/// <summary>The little that a batch runner needs of a command line.</summary>
file sealed class Arguments(Dictionary<string, string?> values)
{
    internal static Arguments Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{args[index]}'.");
            }

            var name = args[index][2..];
            var next = index + 1 < args.Length ? args[index + 1] : null;

            if (next is not null && !next.StartsWith("--", StringComparison.Ordinal))
            {
                values[name] = next;
                index++;
            }
            else
            {
                values[name] = null;
            }
        }

        return new Arguments(values);
    }

    internal bool Has(string name) => values.ContainsKey(name);

    internal bool Flag(string name) => values.TryGetValue(name, out var value) && value is null;

    internal string Value(string name, string fallback) =>
        values.TryGetValue(name, out var value) && value is not null ? value : fallback;

    internal int Number(string name, int fallback) =>
        values.TryGetValue(name, out var value) && value is not null
            ? int.TryParse(value, CultureInfo.InvariantCulture, out var number)
                ? number
                : throw new ArgumentException($"--{name} wants a number, not '{value}'.")
            : fallback;
}
