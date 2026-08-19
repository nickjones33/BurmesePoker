using System.Diagnostics;
using System.Globalization;
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
            + $"{effect.WinRate.Mean * 100,13:+0.0;-0.0} ± {effect.WinRate.Interval * 100,4:0.0}"
            + $"{effect.TakeRate.Mean * 100,13:+0.0;-0.0} ± {effect.TakeRate.Interval * 100,4:0.0}"
            + $"{effect.NetPerRound.Mean,11:+0.00;-0.00} ± {effect.NetPerRound.Interval,4:0.00}"));
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
            + $"{winRate.Mean * 100,13:+0.0;-0.0} ± {winRate.Interval * 100,4:0.0}"
            + $"{takeRate.Mean * 100,13:+0.0;-0.0} ± {takeRate.Interval * 100,4:0.0}"
            + $"  {(winRate.IsSeparatedFromZero ? "separated" : "inside the interval")}"));
    }
}

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

static void Report(SimulationReport report)
{
    Console.WriteLine();
    Console.WriteLine(
        $"{report.Rounds} rounds in {report.Elapsed.TotalSeconds:0.00} s "
        + $"({report.RoundsPerSecond:0} rounds/s), {report.TurnsPerRound:0.0} turns a round, "
        + $"{report.Reshuffles} reshuffle(s), {report.AbandonedGames} game(s) abandoned at the turn cap");
    Console.WriteLine();

    Console.WriteLine(
        $"{"strategy",-10}{"rounds",8}{"wins",8}{"win %",8}{"$/round",10}{"side $/r",10}"
        + $"{"turns",8}{"covered",9}{"take %",8}{"claim %",9}");

    foreach (var strategy in report.Strategies)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{strategy.Name,-10}{strategy.Rounds,8}{strategy.Wins,8}{strategy.WinRate * 100,8:0.0}"
            + $"{strategy.NetPerRound,10:0.00}{strategy.SideBetPerRound,10:0.00}{strategy.TurnsToWin,8:0.0}"
            + $"{strategy.CoveredWhenLosing,9:0.0}{strategy.TakeRate * 100,8:0.0}{strategy.ClaimRate * 100,9:0.0}"));
    }
}

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
    Console.WriteLine($"PartialCover.Best        {Time(dealt, hand => PartialCover.Best(hand).CoveredCount != -1)}");
    Console.WriteLine($"HandEvaluator.TryFindCover {Time(dealt, hand => HandEvaluator.TryFindCover(hand, out _))}");

    return 0;
}

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

static string Usage() => """
    BurmesePoker.Sim — batch play, seeded and parallel.

      dotnet run --project BurmesePoker.Sim -- [options]
      dotnet run --project BurmesePoker.Sim -- bench [--hands N] [--seed N]
      dotnet run --project BurmesePoker.Sim -- replay PATH [--csv PATH]
      dotnet run --project BurmesePoker.Sim -- neighbours [options]

      --strategies a,b   who is playing, rotated through the seats  (greedy,simple)
                         the ladder, weakest first: random, simple, greedy, cautious
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
      --filler NAME      the seats that are neither                 (simple)
      --reference NAME   the level the others are reported against  (greedy)
      --games N          games in each of the 2 x |levels| cells    (2000)
      --seats N, --seed N, --turn-cap N, --serial, --csv PATH  as above

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
