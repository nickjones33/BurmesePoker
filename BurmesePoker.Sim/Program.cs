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

    var options = new SimulationOptions
    {
        Strategies = [.. arguments
            .Value("strategies", "greedy,simple")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(StrategyCatalog.Resolve)],
        Seats = arguments.Number("seats", 4),
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

      --strategies a,b   who is playing, rotated through the seats  (greedy,simple)
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
