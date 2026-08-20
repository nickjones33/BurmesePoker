using System.Diagnostics;

using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>What one cell of a tournament is for.</summary>
public enum CellKind
{
    /// <summary>Two strategies, head to head, at every seating in which both are at the table.</summary>
    Pair,

    /// <summary>Everybody at one table, fully crossed — the question <c>--seating balanced</c> answers.</summary>
    FreeForAll,

    /// <summary>
    /// One strategy against a copy of itself under another name — the harness measuring its own
    /// bias rather than a strategy's strength.
    /// </summary>
    Null
}

/// <summary>
/// Which pairs are played, and so which comparisons the correction is over.
/// </summary>
/// <remarks>
/// 🔥 <b>The family is the claim being made, not the cells that happen to exist</b> (BUILD-PLAN
/// P19 acceptance 1b). A ladder of rungs makes every pairwise claim, so its family is the
/// round-robin. A <em>difficulty dial</em> makes exactly one claim per step — that level
/// <em>n+1</em> beats level <em>n</em> and a person can tell — so its family is the k−1
/// adjacent comparisons, and correcting over all k(k−1)/2 would be a power loss paid for
/// nothing. It also saves the cells: four levels cost three head-to-head runs rather than six.
/// </remarks>
public enum PairFamily
{
    /// <summary>Every unordered pair. What ranking a field means.</summary>
    RoundRobin,

    /// <summary>Only each strategy against the next one named. What a monotone dial claims.</summary>
    Adjacent
}

/// <summary>
/// A round-robin: every strategy against every other, with an interval on every difference and
/// the family of comparisons accounted for (BUILD-PLAN P17).
/// </summary>
public sealed record TournamentOptions
{
    /// <summary>The field. Two or more, with distinct names.</summary>
    public required IReadOnlyList<Strategy> Strategies { get; init; }

    /// <summary>Seats at the table (RULES.md §2.1).</summary>
    public int Seats { get; init; } = RoundEngine.MinimumPlayers;

    /// <summary>
    /// Games wanted in each cell. <b>Rounded up</b> to a whole number of passes through that
    /// cell's seatings, so seat balance is arithmetic rather than luck — see
    /// <see cref="GamesFor"/>.
    /// </summary>
    public int GamesPerCell { get; init; } = 2000;

    /// <summary>
    /// The one number the whole tournament reproduces from. <b>Shared by every cell on
    /// purpose</b>: game <i>i</i> is then dealt from the same shoe in every cell, which is what
    /// makes a comparison across cells a paired one (<see cref="Measurement.Paired"/>).
    /// </summary>
    public int MasterSeed { get; init; } = 20260819;

    /// <summary>Turns before a stalled round is given up on.</summary>
    public int TurnCap { get; init; } = 400;

    /// <summary>What the rounds are played for.</summary>
    public Stakes Stakes { get; init; } = Stakes.Standard;

    /// <summary>Whether the games inside a cell run concurrently. Results are identical either way.</summary>
    public bool Parallel { get; init; } = true;

    /// <summary>The family-wise error rate the correction controls.</summary>
    public double Alpha { get; init; } = Holm.DefaultAlpha;

    /// <summary>
    /// Which pairs are played and corrected over. <b>The order the strategies are named in is
    /// what "adjacent" means</b>, so a dial has to be listed in dial order.
    /// </summary>
    public PairFamily Pairs { get; init; } = PairFamily.RoundRobin;

    /// <summary>
    /// Which strategy plays the null cell. Null takes the last one named, which in ladder order
    /// is the strongest — the rung a reader is most likely to trust.
    /// </summary>
    public string? NullTest { get; init; }

    /// <summary>The strategy the null cell is played by.</summary>
    public Strategy NullTestStrategy =>
        NullTest is null
            ? Strategies[^1]
            : Strategies.FirstOrDefault(s => string.Equals(s.Name, NullTest, StringComparison.OrdinalIgnoreCase))
              ?? throw new ArgumentException($"'{NullTest}' is not one of the tournament's strategies.");

    /// <summary>
    /// How many games a cell of <paramref name="assignments"/> seatings actually plays: the
    /// wanted number rounded <b>up</b> to a whole number of passes.
    /// </summary>
    /// <remarks>
    /// <b>Rounding up rather than warning.</b> The games of a run cycle the seatings in order
    /// (<see cref="SimulationOptions.Seating"/>), so a cell whose game count is not a multiple
    /// of its seating count has played some seatings once more than others — a small seat bias
    /// that nothing downstream can see. Rounding costs at most one pass and makes the balance a
    /// property of the design instead of a thing to check.
    /// </remarks>
    public int GamesFor(int assignments)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(assignments, 1);

        return (GamesPerCell + assignments - 1) / assignments * assignments;
    }

    /// <summary>Throws unless the tournament can be run as described.</summary>
    public TournamentOptions Validated()
    {
        ArgumentNullException.ThrowIfNull(Strategies);
        ArgumentNullException.ThrowIfNull(Stakes);

        if (Strategies.Count < 2)
        {
            throw new ArgumentException("A round-robin of one strategy has nothing to rank.", nameof(Strategies));
        }

        if (Strategies.Select(s => s.Name).Distinct(StringComparer.Ordinal).Count() != Strategies.Count)
        {
            throw new ArgumentException(
                "Two strategies with one name would be tabulated as one player.", nameof(Strategies));
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

        _ = NullTestStrategy;

        return this;
    }
}

/// <summary>One strategy's showing inside one cell.</summary>
/// <param name="Name">Who.</param>
/// <param name="WinRate">Share of the games it declared in, a value per game.</param>
/// <param name="NetPerRound">What it banked.</param>
/// <param name="TakeRate">Share of its acquisitions taken off a discard.</param>
/// <param name="SeatRounds">Seat-rounds by seat index — the balance the design claims.</param>
/// <param name="WinRateByGame">
/// The win rate before it was summarised, a value per game with the seed that game was dealt
/// from. <b>Kept because a summary cannot be paired</b> — a comparison across two cells of the
/// same master seed needs the games themselves (<see cref="Measurement.Paired"/>).
/// </param>
public sealed record CellPlayer(
    string Name,
    Measurement WinRate,
    Measurement NetPerRound,
    Measurement TakeRate,
    IReadOnlyList<int> SeatRounds,
    IReadOnlyList<GameValue> WinRateByGame)
{
    /// <summary>Whether it sat in every seat the same number of times, to within one.</summary>
    public bool IsSeatBalanced => SeatRounds.Count == 0 || SeatRounds.Max() - SeatRounds.Min() <= 1;
}

/// <summary>
/// One cell: a table, a seating scheme, and what everybody at it did.
/// </summary>
/// <param name="Kind">What the cell is for.</param>
/// <param name="Row">The strategy the margin is reported from the point of view of.</param>
/// <param name="Column">Its opponent, or empty in a free-for-all.</param>
/// <param name="Assignments">How many seatings the cell cycles through.</param>
/// <param name="Games">Games played.</param>
/// <param name="Settled">Games that reached a declaration and so produced a row.</param>
/// <param name="Abandoned">Games given up at the turn cap. Reported, never dropped (P12).</param>
/// <param name="Players">Everybody at the table.</param>
/// <param name="Margin">
/// Row's win rate less column's, <b>game by game</b> (<see cref="Measurement.Paired"/>). Empty
/// for a free-for-all, which has no two sides.
/// </param>
/// <param name="IndependentMargin">
/// The same difference computed as if the two sides were independent samples. <b>Kept only to
/// be compared with <see cref="Margin"/></b> (P17 acceptance 3): within a cell it is the wrong
/// answer, and how wrong is worth printing.
/// </param>
public sealed record TournamentCell(
    CellKind Kind,
    string Row,
    string Column,
    int Assignments,
    int Games,
    int Settled,
    int Abandoned,
    IReadOnlyList<CellPlayer> Players,
    Measurement Margin,
    Measurement IndependentMargin)
{
    /// <summary>How the cell reads in a report.</summary>
    public string Label => Kind switch
    {
        CellKind.Pair => $"{Row} vs {Column}",
        CellKind.Null => $"{Row} vs itself",
        _ => "free-for-all"
    };

    /// <summary>One player by name.</summary>
    public CellPlayer Player(string name) =>
        Players.FirstOrDefault(player => player.Name == name)
        ?? throw new ArgumentException($"'{name}' did not play the {Label} cell.", nameof(name));
}

/// <summary>
/// A pairing put beside the independent formula on the same data (P17 acceptance 3).
/// </summary>
/// <param name="Label">What was compared.</param>
/// <param name="Scope">
/// <c>within-cell</c> — two strategies at one table — or <c>across-cells</c> — one strategy at
/// two tables of the same master seed. ⚠️ <b>The two point opposite ways</b>, which is the
/// finding.
/// </param>
/// <param name="Paired">The per-game difference.</param>
/// <param name="Independent">The same difference with the variances simply added.</param>
public sealed record PairingCheck(string Label, string Scope, Measurement Paired, Measurement Independent)
{
    /// <summary>Paired standard error over independent. Below one is variance reduction.</summary>
    public double Ratio => Independent.StandardError > 0 ? Paired.StandardError / Independent.StandardError : 0;
}

/// <summary>Where a strategy finished.</summary>
/// <param name="Name">Who.</param>
/// <param name="FreeForAllWinRate">Its win rate with everybody at one table.</param>
/// <param name="MeanMargin">
/// Its average head-to-head margin over the field, in points of win rate. <b>This is what the
/// ranking is by</b> — a free-for-all win rate depends on who else is in the field, while a
/// mean over pairwise cells weights every opponent once.
/// </param>
/// <param name="Beaten">Opponents it beat by a margin that survived the correction.</param>
/// <param name="LostTo">Opponents that beat it, likewise.</param>
/// <param name="Undecided">Opponents it is not separated from.</param>
public sealed record Standing(
    string Name,
    Measurement FreeForAllWinRate,
    double MeanMargin,
    int Beaten,
    int LostTo,
    int Undecided);

/// <summary>
/// The tournament: every unordered pair of the field head to head, everybody at one table
/// beside it, and the harness's own null test (BUILD-PLAN P17).
/// </summary>
/// <remarks>
/// <para>
/// <b>The two designs answer different questions and both are reported.</b> A head-to-head cell
/// asks "which of these two plays better", weighting every opponent once; a free-for-all asks
/// "who wins at this table", which depends on who else is at it. P16 is the packet that learned
/// they differ — a rotation flattered <c>greedy</c> by a fifth of its own gap — so neither is
/// allowed to stand in for the other here.
/// </para>
/// <para>
/// ⚠️ <b>A round-robin manufactures findings and this class refuses to.</b> Every pairwise
/// margin goes through <see cref="Holm"/>, and the raw verdict is printed beside the corrected
/// one rather than replaced by it (see <see cref="Holm"/> for why six comparisons are more
/// likely than not to produce a spurious one).
/// </para>
/// <para>
/// <b>Nothing here changes the domain, the simulator or the runner</b> — a cell is a
/// <see cref="SimulationOptions"/> with an explicit seating, which P16 already built.
/// </para>
/// </remarks>
public static class Tournament
{
    /// <summary>The suffix the null cell's second label wears.</summary>
    /// <remarks>
    /// <b>A copy of a strategy needs its own name to be tabulated at all</b> — the summary
    /// tallies by name — and the name has to be one no ladder will ever use. It is not a rung
    /// and it never appears outside a null cell.
    /// </remarks>
    public const string MirrorSuffix = "#mirror";

    /// <summary>Plays every cell, corrects the family, and ranks the field.</summary>
    public static TournamentReport Run(TournamentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validated();

        var clock = Stopwatch.StartNew();

        var pairs = new List<TournamentCell>();

        for (var row = 0; row < options.Strategies.Count; row++)
        {
            for (var column = row + 1; column < options.Strategies.Count; column++)
            {
                if (options.Pairs == PairFamily.Adjacent && column != row + 1)
                {
                    continue;
                }

                pairs.Add(Play(options, CellKind.Pair, options.Strategies[row], options.Strategies[column]));
            }
        }

        var freeForAll = PlayFreeForAll(options);
        var nullCell = PlayNull(options);

        clock.Stop();

        var verdicts = Holm.Correct(
            [.. pairs.Select(cell => (cell.Label, cell.Margin))],
            options.Alpha);

        return new TournamentReport(
            options,
            pairs,
            freeForAll,
            nullCell,
            verdicts,
            Standings(options, pairs, freeForAll, verdicts),
            PairingChecks(options, pairs),
            clock.Elapsed);
    }

    /// <summary>The run one cell is, ready to be played or inspected.</summary>
    public static SimulationOptions RunOf(
        TournamentOptions options,
        IReadOnlyList<Strategy> players,
        IReadOnlyList<IReadOnlyList<Strategy>> assignments) =>
        new SimulationOptions
        {
            Strategies = players,
            Assignments = assignments,
            Seats = options.Seats,
            Games = options.GamesFor(assignments.Count),
            RoundsPerGame = 1,
            MasterSeed = options.MasterSeed,
            Stakes = options.Stakes,
            TurnCap = options.TurnCap,
            Parallel = options.Parallel
        }.Validated();

    /// <summary>The two labels a null cell seats: a strategy, and the same one under a name of its own.</summary>
    public static IReadOnlyList<Strategy> NullPair(Strategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);

        return [strategy, strategy with { Name = strategy.Name + MirrorSuffix }];
    }

    private static TournamentCell Play(TournamentOptions options, CellKind kind, Strategy row, Strategy column)
    {
        IReadOnlyList<Strategy> players = [row, column];

        return Measure(
            options,
            kind,
            row.Name,
            column.Name,
            SeatingPlan.HeadToHead(players, options.Seats),
            players);
    }

    private static TournamentCell PlayFreeForAll(TournamentOptions options) =>
        Measure(
            options,
            CellKind.FreeForAll,
            string.Empty,
            string.Empty,
            SeatingPlan.Balanced(options.Strategies, options.Seats),
            options.Strategies);

    private static TournamentCell PlayNull(TournamentOptions options)
    {
        var pair = NullPair(options.NullTestStrategy);

        return Measure(
            options,
            CellKind.Null,
            pair[0].Name,
            pair[1].Name,
            SeatingPlan.HeadToHead(pair, options.Seats),
            pair);
    }

    private static TournamentCell Measure(
        TournamentOptions options,
        CellKind kind,
        string row,
        string column,
        IReadOnlyList<IReadOnlyList<Strategy>> assignments,
        IReadOnlyList<Strategy> players)
    {
        var run = RunOf(options, players, assignments);
        var report = Simulator.Run(run);
        var series = StrategySeries.Of(report);

        var settled = report.Games.Count(game => game.Rounds.Count > 0);

        var cellPlayers = series
            .Select(one => new CellPlayer(
                one.Name,
                Measurement.Of(one.WinRate),
                Measurement.Of(one.NetPerRound),
                Measurement.Of(one.TakeRate),
                one.SeatRounds,
                one.WinRate))
            .ToList();

        if (kind == CellKind.FreeForAll)
        {
            return new TournamentCell(
                kind, row, column, assignments.Count, report.Games.Count, settled,
                report.Games.Count - settled, cellPlayers, default, default);
        }

        var rowSeries = series.Single(one => one.Name == row);
        var columnSeries = series.Single(one => one.Name == column);

        return new TournamentCell(
            kind,
            row,
            column,
            assignments.Count,
            report.Games.Count,
            settled,
            report.Games.Count - settled,
            cellPlayers,
            Measurement.Paired(rowSeries.WinRate, columnSeries.WinRate),
            Measurement.Difference(Measurement.Of(rowSeries.WinRate), Measurement.Of(columnSeries.WinRate)));
    }

    private static IReadOnlyList<Standing> Standings(
        TournamentOptions options,
        IReadOnlyList<TournamentCell> pairs,
        TournamentCell freeForAll,
        IReadOnlyList<HolmVerdict> verdicts)
    {
        var standings = new List<Standing>(options.Strategies.Count);

        foreach (var strategy in options.Strategies)
        {
            var margins = new List<double>();
            var beaten = 0;
            var lostTo = 0;
            var undecided = 0;

            for (var index = 0; index < pairs.Count; index++)
            {
                var cell = pairs[index];

                if (cell.Row != strategy.Name && cell.Column != strategy.Name)
                {
                    continue;
                }

                // A cell is stored once, from the row's point of view; the column reads it
                // backwards. Getting this sign wrong produces a perfectly plausible ranking.
                var sign = cell.Row == strategy.Name ? 1 : -1;

                margins.Add(sign * cell.Margin.Mean);

                if (!verdicts[index].Survives)
                {
                    undecided++;
                }
                else if (sign * cell.Margin.Mean > 0)
                {
                    beaten++;
                }
                else
                {
                    lostTo++;
                }
            }

            standings.Add(new Standing(
                strategy.Name,
                freeForAll.Player(strategy.Name).WinRate,
                margins.Count == 0 ? 0 : margins.Sum() / margins.Count,
                beaten,
                lostTo,
                undecided));
        }

        return
        [
            .. standings
                .OrderByDescending(standing => standing.MeanMargin)
                .ThenBy(standing => standing.Name, StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// The pairing put beside the independent formula, in both directions it can point.
    /// </summary>
    /// <remarks>
    /// <b>Within a cell</b> every pairwise margin is already both. <b>Across cells</b> the
    /// comparison is one strategy's win rate at two different tables of the same master seed —
    /// the shape P16's effects have, and the shape common random numbers were meant for. Each
    /// strategy contributes one such check, against its first and last opponent in the order
    /// the field was named, so the set is deterministic.
    /// </remarks>
    private static IReadOnlyList<PairingCheck> PairingChecks(
        TournamentOptions options,
        IReadOnlyList<TournamentCell> pairs)
    {
        var checks = new List<PairingCheck>();

        foreach (var cell in pairs)
        {
            checks.Add(new PairingCheck(cell.Label, "within-cell", cell.Margin, cell.IndependentMargin));
        }

        foreach (var strategy in options.Strategies)
        {
            // ⚠️ Both cells must seat this strategy the same way round. A head-to-head cell
            // enumerates its seatings as an odometer over its two players in list order, so the
            // strategy named *first* sits in the same seats in assignment k of every cell it
            // leads. Comparing a cell it leads with one it follows in would move it round the
            // table between the two, and the shoes being shared would buy nothing — the
            // measurement would be honest and the demonstration pointless.
            //
            // ⚠️ Asked of the cells that were played rather than of the field (BUILD-PLAN P19):
            // under --pairs adjacent most pairs never meet, and every strategy leads at most
            // one cell — so this check simply has nothing to compare and says so by being
            // absent, rather than by throwing on a cell that is not there.
            var led = pairs.Where(cell => cell.Row == strategy.Name).Select(cell => cell.Column).ToList();
            var followed = pairs.Where(cell => cell.Column == strategy.Name).Select(cell => cell.Row).ToList();
            var matched = led.Count >= 2 ? led : followed;

            if (matched.Count < 2)
            {
                continue;
            }

            var first = SeriesOf(pairs, strategy.Name, matched[0]);
            var second = SeriesOf(pairs, strategy.Name, matched[1]);

            checks.Add(new PairingCheck(
                $"{strategy.Name}: vs {matched[0]} less vs {matched[1]}",
                "across-cells",
                Measurement.Paired(first.Values, second.Values),
                Measurement.Difference(first.Summary, second.Summary)));
        }

        return checks;
    }

    private static (IReadOnlyList<GameValue> Values, Measurement Summary) SeriesOf(
        IReadOnlyList<TournamentCell> pairs,
        string strategy,
        string opponent)
    {
        var cell = pairs.Single(one =>
            (one.Row == strategy && one.Column == opponent) || (one.Row == opponent && one.Column == strategy));

        var player = cell.Player(strategy);

        return (player.WinRateByGame, player.WinRate);
    }
}

/// <summary>
/// What the tournament found: every cell, every corrected comparison, and the ranking.
/// </summary>
/// <param name="Options">The run's inputs, so the report is self-describing.</param>
/// <param name="Pairs">Every unordered pair, in named order — row before column.</param>
/// <param name="FreeForAll">Everybody at one table.</param>
/// <param name="NullCell">A strategy against a copy of itself.</param>
/// <param name="Verdicts">One per pair cell, <b>in the same order</b>, Holm-corrected.</param>
/// <param name="Ranking">The field, strongest first.</param>
/// <param name="Pairing">Paired against independent, on the same data (P17 acceptance 3).</param>
/// <param name="Elapsed">Wall clock — the only field that changes between two identical runs.</param>
public sealed record TournamentReport(
    TournamentOptions Options,
    IReadOnlyList<TournamentCell> Pairs,
    TournamentCell FreeForAll,
    TournamentCell NullCell,
    IReadOnlyList<HolmVerdict> Verdicts,
    IReadOnlyList<Standing> Ranking,
    IReadOnlyList<PairingCheck> Pairing,
    TimeSpan Elapsed)
{
    /// <summary>How many comparisons the correction had to account for.</summary>
    public int Comparisons => Pairs.Count;

    /// <summary>Every game the tournament played.</summary>
    public int Games => Pairs.Sum(cell => cell.Games) + FreeForAll.Games + NullCell.Games;

    /// <summary>
    /// Whether the harness's own null test passed: a strategy against a copy of itself wins its
    /// share of the seats, and neither copy beats the other.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>This is the cell that would have caught P16's seating artifact from the inside.</b>
    /// Two labels of one strategy differ in nothing at all, so any gap between them is the
    /// apparatus — a seat that opens, a seating that is not balanced, a series that counts
    /// seats as games. It costs one cell and it is checked on every run.
    /// </remarks>
    public bool NullTestHolds
    {
        get
        {
            var fair = 1.0 / Options.Seats;

            return NullCell.Players.All(player => Math.Abs(player.WinRate.Mean - fair) <= player.WinRate.Interval)
                && !NullCell.Margin.IsSeparatedFromZero;
        }
    }

    /// <summary>The margin of <paramref name="row"/> over <paramref name="column"/>, either way round.</summary>
    public (Measurement Margin, HolmVerdict Verdict) Head(string row, string column)
    {
        for (var index = 0; index < Pairs.Count; index++)
        {
            var cell = Pairs[index];

            if (cell.Row == row && cell.Column == column)
            {
                return (cell.Margin, Verdicts[index]);
            }

            if (cell.Row == column && cell.Column == row)
            {
                var margin = cell.Margin;

                // Negating a zero mean gives negative zero, which a two-section format prints
                // as "-+0.0". Read the other way round, a dead heat is still a dead heat.
                return (margin with { Mean = margin.Mean == 0 ? 0 : -margin.Mean }, Verdicts[index]);
            }
        }

        throw new ArgumentException($"'{row}' and '{column}' did not meet.", nameof(row));
    }

    /// <summary>
    /// Whether those two played a cell at all.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>They always do in a round-robin and mostly do not under
    /// <see cref="PairFamily.Adjacent"/></b>, so a report that draws the matrix has to ask
    /// before it reads a cell (BUILD-PLAN P19).
    /// </remarks>
    public bool Met(string row, string column) =>
        Pairs.Any(cell =>
            (cell.Row == row && cell.Column == column) || (cell.Row == column && cell.Column == row));
}
