using System.Diagnostics;
using System.Globalization;

using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>
/// One run of the money sweep: two rungs, head to head, at several stakes ratios
/// (BUILD-PLAN P22).
/// </summary>
public sealed record MoneySweepOptions
{
    /// <summary>The rung whose take decision reads the side bet.</summary>
    public required Strategy Challenger { get; init; }

    /// <summary>
    /// The rung it is one change away from. ⚠️ <b>Name it explicitly and never assume it is
    /// <c>greedy</c></b>: P21 promoted <c>outs</c> to the top of the ladder, so "greedy,
    /// except…" stopped being "the best rung, except…".
    /// </summary>
    public required Strategy Reference { get; init; }

    /// <summary>
    /// The stakes to sweep, <b>in the order they are to be reported</b> — cheapest side bet
    /// first, so a reader walks the dial rather than a set.
    /// </summary>
    public required IReadOnlyList<Stakes> Ratios { get; init; }

    /// <summary>Seats at the table (RULES.md §2.1).</summary>
    public int Seats { get; init; } = RoundEngine.MinimumPlayers;

    /// <summary>Games wanted in each cell, rounded up to a whole number of seatings.</summary>
    public int GamesPerCell { get; init; } = 2000;

    /// <summary>
    /// The one number the whole sweep reproduces from. <b>Shared by every cell on purpose</b>:
    /// game <i>i</i> is dealt from the same shoe at every stakes ratio, so the ratios differ in
    /// the stakes and in nothing else.
    /// </summary>
    public int MasterSeed { get; init; } = 20260819;

    /// <summary>Turns before a stalled round is given up on.</summary>
    public int TurnCap { get; init; } = 400;

    /// <summary>Whether the games inside a cell run concurrently. Results are identical either way.</summary>
    public bool Parallel { get; init; } = true;

    /// <summary>The family-wise error rate the correction controls.</summary>
    public double Alpha { get; init; } = Holm.DefaultAlpha;

    /// <summary>Throws unless the sweep can be run as described.</summary>
    public MoneySweepOptions Validated()
    {
        ArgumentNullException.ThrowIfNull(Challenger);
        ArgumentNullException.ThrowIfNull(Reference);
        ArgumentNullException.ThrowIfNull(Ratios);

        if (string.Equals(Challenger.Name, Reference.Name, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A rung against itself is a null cell, not a comparison.", nameof(Reference));
        }

        if (Ratios.Count < 3)
        {
            throw new ArgumentException(
                "The answer is expected to depend on the stakes, so a sweep is at least three "
                + "ratios (BUILD-PLAN P22 acceptance 1).",
                nameof(Ratios));
        }

        if (Ratios.Select(stakes => (stakes.RoundValue, stakes.MoneyCardValue)).Distinct().Count() != Ratios.Count)
        {
            throw new ArgumentException("Two cells of the same stakes measure one thing twice.", nameof(Ratios));
        }

        if (Seats is < RoundEngine.MinimumPlayers or > RoundEngine.MaximumPlayers)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Seats),
                Seats,
                $"A round is for {RoundEngine.MinimumPlayers} to {RoundEngine.MaximumPlayers} players (RULES.md §2.1).");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(GamesPerCell, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(TurnCap, 1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Alpha);

        return this;
    }
}

/// <summary>
/// One player's showing inside one cell of the sweep.
/// </summary>
/// <remarks>
/// ⚠️ <b>A <see cref="CellPlayer"/> with the side bet split out</b>, which is the whole reason
/// this type exists: the tournament ranks by win rate and never needs it, and this packet is
/// about the half of the money that settles independently of who won (RULES.md §4.3).
/// </remarks>
/// <param name="Name">Who.</param>
/// <param name="WinRate">Share of the games it declared in, a value per game.</param>
/// <param name="NetPerRound">What it banked, both halves together — <b>what the packet is judged on</b>.</param>
/// <param name="SideBetPerRound">The money-card half alone, where the mechanism has to show up.</param>
/// <param name="TakeRate">
/// Share of its acquisitions taken off a discard rather than drawn blind. <b><c>prospector</c>'s
/// mechanism variable</b>: if a rule about the draw fires at all, this is what moves first.
/// </param>
/// <param name="CleanWinRate">
/// Share of its <b>own wins</b> declared with no joker in the thirteen (RULES.md §7.3) —
/// <b><c>purist</c>'s mechanism variable</b> (BUILD-PLAN P44), a conditional series like
/// <c>TurnsToWin</c>: only games this player won have trials. For every rung that is not
/// playing for the bonus it reads near §14's accidental floor, which is the control.
/// </param>
/// <param name="SeatRounds">Seat-rounds by seat index — the balance the design claims.</param>
public sealed record MoneyPlayer(
    string Name,
    Measurement WinRate,
    Measurement NetPerRound,
    Measurement SideBetPerRound,
    Measurement TakeRate,
    Measurement CleanWinRate,
    IReadOnlyList<int> SeatRounds)
{
    /// <summary>Whether it sat in every seat the same number of times, to within one.</summary>
    public bool IsSeatBalanced => SeatRounds.Count == 0 || SeatRounds.Max() - SeatRounds.Min() <= 1;
}

/// <summary>
/// One stakes ratio: the challenger against the reference at every seating in which both are at
/// the table.
/// </summary>
/// <param name="Stakes">What the rounds were played for.</param>
/// <param name="Assignments">How many seatings the cell cycles through.</param>
/// <param name="Games">Games played.</param>
/// <param name="Settled">Games that reached a declaration and so produced a row.</param>
/// <param name="Abandoned">Games given up at the turn cap. Reported, never dropped (P12).</param>
/// <param name="Players">Everybody at the table, challenger first.</param>
/// <param name="NetMargin">
/// 🔥 <b>The figure the packet is judged on.</b> The challenger's money per round less the
/// reference's, <b>game by game</b> (<see cref="Measurement.Paired"/>).
/// </param>
/// <param name="SideMargin">The money-card half of it alone — where the mechanism has to show up.</param>
/// <param name="WinMargin">
/// The same margin in win rate, reported beside the money because <b>this is the first packet
/// where the two can come apart</b> (P22 acceptance 2).
/// </param>
/// <param name="Rounds">Rounds that settled — one per settled game at <c>RoundsPerGame = 1</c>.</param>
/// <param name="JokerlessRounds">
/// Of those, how many the winner declared jokerless (RULES.md §7.3) — the cell-level twin of
/// the <c>bonus.jokerless-rate.*</c> rows the tournaments publish, so a money cell can be read
/// beside <c>docs/STRATEGY.md</c> §14's floor (BUILD-PLAN P44).
/// </param>
public sealed record MoneyCell(
    Stakes Stakes,
    int Assignments,
    int Games,
    int Settled,
    int Abandoned,
    IReadOnlyList<MoneyPlayer> Players,
    Measurement NetMargin,
    Measurement SideMargin,
    Measurement WinMargin,
    int Rounds = 0,
    int JokerlessRounds = 0)
{
    /// <summary>Share of the settled rounds won jokerless in this cell (RULES.md §7.3).</summary>
    public double JokerlessRate => Rounds == 0 ? 0 : (double)JokerlessRounds / Rounds;

    /// <summary>The money card value as a multiple of the round value — the dial being swept.</summary>
    public double Ratio => (double)Stakes.MoneyCardValue / Stakes.RoundValue;

    /// <summary>How the cell reads in a report: <c>$5/$1</c>.</summary>
    public string Label => string.Create(
        CultureInfo.InvariantCulture, $"${Stakes.RoundValue}/${Stakes.MoneyCardValue}");

    /// <summary>A key a CSV row and a document can both quote.</summary>
    public string Id => string.Create(
        CultureInfo.InvariantCulture, $"{Stakes.RoundValue}-{Stakes.MoneyCardValue}");

    /// <summary>One player by name.</summary>
    public MoneyPlayer Player(string name) =>
        Players.FirstOrDefault(player => player.Name == name)
        ?? throw new ArgumentException($"'{name}' did not play the {Label} cell.", nameof(name));

    /// <summary>
    /// Whether money and win rate disagree about who played better — a rung that wins fewer
    /// rounds and banks more.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Stated by the report rather than left for a reader to notice</b> (P22 acceptance 2).
    /// Both sides have to clear their own interval before a disagreement is claimed: two
    /// unseparated numbers pointing opposite ways are one number pointing nowhere.
    /// </remarks>
    public bool MoneyAndWinRateDisagree =>
        NetMargin.IsSeparatedFromZero
        && WinMargin.IsSeparatedFromZero
        && Math.Sign(NetMargin.Mean) != Math.Sign(WinMargin.Mean);
}

/// <summary>
/// The sweep: <em>should you draw blind for the money?</em>, asked at several stakes
/// (BUILD-PLAN P22).
/// </summary>
/// <remarks>
/// <para>
/// <b>The stakes are the independent variable and that is the whole design.</b> Whether the side
/// bet is worth playing for is not a fact about the rules but a fact about the ratio of
/// <see cref="Stakes.MoneyCardValue"/> to <see cref="Stakes.RoundValue"/>, so one cell at the
/// standard 5:1 would answer only "not at these stakes" and could not say how far away the
/// answer is. Every cell shares a master seed, so game <i>i</i> is dealt from the same shoe at
/// every ratio and the cells differ in the money and in nothing else.
/// </para>
/// <para>
/// ⚠️ <b>Judged on <c>$/round</c>, with the win rate printed beside it.</b> A rung that wins
/// fewer rounds and banks more money is the better player, and the harness has split the flat
/// prize from the side bet since P12 precisely so that the two could be seen to come apart.
/// </para>
/// <para>
/// <b>Nothing here changes the domain, the simulator or the tournament</b> — a cell is a
/// <see cref="SimulationOptions"/> with an explicit seating and an explicit
/// <see cref="Stakes"/>, both of which P12 already built.
/// </para>
/// </remarks>
public static class MoneySweep
{
    /// <summary>Where the sweep writes unless told otherwise.</summary>
    public const string DefaultPath = "docs/strategy/money.csv";

    /// <summary>The ratios the sweep walks unless told otherwise.</summary>
    /// <remarks>
    /// <b>The first is the game as it is actually played</b> (RULES.md §4.3), and the rest walk
    /// the money card up until it is worth more than the round it is settled beside. They are
    /// chosen to straddle the point where the challenger's rule starts firing at all, which at
    /// four seats is a money card worth about seven times a round.
    /// </remarks>
    public static IReadOnlyList<Stakes> DefaultRatios { get; } =
    [
        Stakes.Standard,
        new(5, 10),
        new(5, 20),
        new(5, 40)
    ];

    /// <summary>Plays every ratio and corrects the family of money margins.</summary>
    public static MoneyReport Run(MoneySweepOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validated();

        var clock = Stopwatch.StartNew();

        var cells = new List<MoneyCell>(options.Ratios.Count);

        foreach (var stakes in options.Ratios)
        {
            cells.Add(Cell(options, stakes));
        }

        clock.Stop();

        // ⚠️ The family is the sweep. One ratio in four clearing a 95% interval by luck is the
        // ordinary outcome of asking four questions, and the packet's whole claim is that the
        // answer moves with the stakes — so the money margins are corrected together.
        var verdicts = Holm.Correct(
            [.. cells.Select(cell => (cell.Label, cell.NetMargin))],
            options.Alpha);

        return new MoneyReport(options, cells, verdicts, clock.Elapsed);
    }

    /// <summary>The run one cell is, ready to be played or inspected.</summary>
    public static SimulationOptions RunOf(MoneySweepOptions options, Stakes stakes)
    {
        ArgumentNullException.ThrowIfNull(options);

        IReadOnlyList<Strategy> players = [options.Challenger, options.Reference];
        var assignments = SeatingPlan.HeadToHead(players, options.Seats);

        return new SimulationOptions
        {
            Strategies = players,
            Assignments = assignments,
            Seats = options.Seats,
            Games = (options.GamesPerCell + assignments.Count - 1) / assignments.Count * assignments.Count,
            RoundsPerGame = 1,
            MasterSeed = options.MasterSeed,
            Stakes = stakes,
            TurnCap = options.TurnCap,
            Parallel = options.Parallel
        }.Validated();
    }

    private static MoneyCell Cell(MoneySweepOptions options, Stakes stakes)
    {
        var run = RunOf(options, stakes);
        var report = Simulator.Run(run);
        var series = StrategySeries.Of(report);
        var clean = CleanWins(report);

        var settled = report.Games.Count(game => game.Rounds.Count > 0);
        var rounds = report.Games.Sum(game => game.Rounds.Count);
        var jokerless = report.Games.Sum(game => game.Rounds.Count(round => round.Jokerless));

        var players = series
            .Select(one => new MoneyPlayer(
                one.Name,
                Measurement.Of(one.WinRate),
                Measurement.Of(one.NetPerRound),
                Measurement.Of(one.SideBetPerRound),
                Measurement.Of(one.TakeRate),
                Measurement.Of(clean[one.Name]),
                one.SeatRounds))
            .ToList();

        var challenger = series.Single(one => one.Name == options.Challenger.Name);
        var reference = series.Single(one => one.Name == options.Reference.Name);

        return new MoneyCell(
            stakes,
            run.Assignments!.Count,
            report.Games.Count,
            settled,
            report.Games.Count - settled,
            players,
            Measurement.Paired(challenger.NetPerRound, reference.NetPerRound),
            Measurement.Paired(challenger.SideBetPerRound, reference.SideBetPerRound),
            Measurement.Paired(challenger.WinRate, reference.WinRate),
            rounds,
            jokerless);
    }

    /// <summary>
    /// Each strategy's clean wins, a value per game: how many of the rounds it won this game
    /// were declared jokerless, out of the rounds it won at all.
    /// </summary>
    /// <remarks>
    /// <b>Arithmetic over rows the run already produced</b> (BUILD-PLAN §3.8) — the winner is
    /// the seat whose <see cref="SeatRow.Won"/> is set, and cleanliness is the round's own
    /// <see cref="RoundRow.Jokerless"/>. A game a strategy never won contributes a value with no
    /// trials, which <see cref="Measurement.Of(IReadOnlyList{GameValue})"/> drops rather than
    /// counts — the same conditional-series rule <c>TurnsToWin</c> follows.
    /// </remarks>
    private static Dictionary<string, List<GameValue>> CleanWins(SimulationReport report)
    {
        var series = report.Options.Strategies.ToDictionary(
            strategy => strategy.Name,
            _ => new List<GameValue>(report.Games.Count),
            StringComparer.Ordinal);

        foreach (var game in report.Games)
        {
            if (game.Rounds.Count == 0)
            {
                continue;
            }

            var wins = new Dictionary<string, (int Won, int Clean)>(StringComparer.Ordinal);

            foreach (var round in game.Rounds)
            {
                foreach (var seat in round.Seats)
                {
                    var tally = wins.TryGetValue(seat.Strategy, out var held) ? held : (0, 0);

                    if (seat.Won)
                    {
                        tally = (tally.Item1 + 1, tally.Item2 + (round.Jokerless ? 1 : 0));
                    }

                    wins[seat.Strategy] = tally;
                }
            }

            foreach (var (name, tally) in wins)
            {
                series[name].Add(new GameValue(game.Seed, tally.Clean, tally.Won));
            }
        }

        return series;
    }
}

/// <summary>What the sweep found: every ratio, and the corrected family of money margins.</summary>
/// <param name="Options">The run's inputs, so the report is self-describing.</param>
/// <param name="Cells">One per stakes ratio, in the order they were named.</param>
/// <param name="Verdicts">One per cell, <b>in the same order</b>, Holm-corrected.</param>
/// <param name="Elapsed">Wall clock — the only field that changes between two identical runs.</param>
public sealed record MoneyReport(
    MoneySweepOptions Options,
    IReadOnlyList<MoneyCell> Cells,
    IReadOnlyList<HolmVerdict> Verdicts,
    TimeSpan Elapsed)
{
    /// <summary>Every game the sweep played.</summary>
    public int Games => Cells.Sum(cell => cell.Games);

    /// <summary>
    /// The cheapest side bet at which the challenger's money margin survives the correction in
    /// its favour, or null if it never does.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>This is the answer the packet owes</b> — "should you draw blind for the money?"
    /// takes a number and the stakes it depends on, not a yes or a no. A sweep that never
    /// crosses says so by returning nothing, which is a publishable result and not a gap.
    /// </remarks>
    public Stakes? PaysFrom
    {
        get
        {
            for (var index = 0; index < Cells.Count; index++)
            {
                if (Verdicts[index].Survives && Cells[index].NetMargin.Mean > 0)
                {
                    return Cells[index].Stakes;
                }
            }

            return null;
        }
    }

    /// <summary>Every ratio at which the money and the win rate point opposite ways.</summary>
    public IReadOnlyList<MoneyCell> Divergences =>
        [.. Cells.Where(cell => cell.MoneyAndWinRateDisagree)];
}

/// <summary>The sweep as a file (BUILD-PLAN §3.12: a number carries the command that made it).</summary>
public static class MoneyCsv
{
    /// <summary>Every cell, one to a row.</summary>
    public static IEnumerable<string> Rows(MoneyReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        yield return
            "round_value,money_card_value,ratio,games,settled,abandoned,"
            + "challenger,challenger_net,challenger_side,challenger_win,challenger_take,challenger_clean,"
            + "reference,reference_net,reference_side,reference_win,reference_take,reference_clean,"
            + "net_margin,net_interval,side_margin,side_interval,win_margin,win_interval,jokerless_rate,p,verdict";

        for (var index = 0; index < report.Cells.Count; index++)
        {
            var cell = report.Cells[index];
            var verdict = report.Verdicts[index];
            var challenger = cell.Player(report.Options.Challenger.Name);
            var reference = cell.Player(report.Options.Reference.Name);

            yield return string.Create(CultureInfo.InvariantCulture,
                $"{cell.Stakes.RoundValue},{cell.Stakes.MoneyCardValue},{Number(cell.Ratio)},"
                + $"{cell.Games},{cell.Settled},{cell.Abandoned},"
                + $"{challenger.Name},{Number(challenger.NetPerRound.Mean)},"
                + $"{Number(challenger.SideBetPerRound.Mean)},{Number(challenger.WinRate.Mean)},"
                + $"{Number(challenger.TakeRate.Mean)},{Number(challenger.CleanWinRate.Mean)},"
                + $"{reference.Name},{Number(reference.NetPerRound.Mean)},"
                + $"{Number(reference.SideBetPerRound.Mean)},{Number(reference.WinRate.Mean)},"
                + $"{Number(reference.TakeRate.Mean)},{Number(reference.CleanWinRate.Mean)},"
                + $"{Number(cell.NetMargin.Mean)},{Number(cell.NetMargin.Interval)},"
                + $"{Number(cell.SideMargin.Mean)},{Number(cell.SideMargin.Interval)},"
                + $"{Number(cell.WinMargin.Mean)},{Number(cell.WinMargin.Interval)},"
                + $"{Number(cell.JokerlessRate)},"
                + $"{Number(verdict.PValue)},{verdict.Survives switch
                {
                    true => "survives",
                    false when verdict.Separated => "raw only",
                    _ => "inside the interval"
                }}");
        }
    }

    /// <summary>Writes them, making the directory if it is not there.</summary>
    public static void WriteTo(string path, MoneyReport report)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, Rows(report));
    }

    private static string Number(double value) =>
        double.IsFinite(value)
            ? value.ToString("0.000000", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
