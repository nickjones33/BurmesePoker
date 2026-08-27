using BurmesePoker.Domain.Agents;

using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// ✅ <b>P48 — the measurement-hardening readouts, held to the code by a test.</b>
/// </summary>
/// <remarks>
/// <para>
/// The runs here are small and the numbers they produce mean nothing — the published figures come
/// from thousands of games at the command line and live in <c>docs/strategy/measurements.csv</c>.
/// What a test can check is that the new estimators are the estimators they claim to be: that a
/// composition stratum is a decomposition of the pooled margin (F1), that a money margin is a
/// paired difference in dollars (F2), that a field rate now carries the interval its ratio implies
/// (F7), that a bootstrap is a deterministic function of its seed and brackets the point (F6), and
/// that the replication reads a margin the same way at two seeds (F5).
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class HardeningTests
{
    private static readonly TournamentOptions Small = new()
    {
        Strategies = [.. new[] { "simple", "greedy", "cautious" }.Select(StrategyCatalog.Resolve)],
        Seats = 4,
        GamesPerCell = 96,
        MasterSeed = 20260819,
        CountLockBites = true,
        Parallel = false
    };

    private static readonly Lazy<TournamentReport> Played = new(() => Tournament.Run(Small));

    [Fact]
    public void ACompositionStratumIsTheMarginWithinOneSeatingMix()
    {
        // 🔥 F1. A head-to-head cell pools every seating in which both sit; the strata partition
        // those same GAMES by how many seats the row held. So the counts of the strata add up to
        // the pooled count — nothing double-counted, nothing dropped — and the compositions
        // outside the range (nobody, or everybody) have nothing to measure. (The margins do not
        // add up the same way — the pooled figure is a ratio-of-sums difference — which is why the
        // strata are read against each other, not against the pool.)
        var cell = Played.Value.Pairs[0];

        Assert.Equal(CellKind.Pair, cell.Kind);
        Assert.Equal(default, cell.MarginAtComposition(0));
        Assert.Equal(default, cell.MarginAtComposition(Small.Seats));

        var strata = Enumerable.Range(1, Small.Seats - 1)
            .Select(cell.MarginAtComposition)
            .Where(stratum => stratum.Count > 0)
            .ToList();

        Assert.NotEmpty(strata);
        Assert.Equal(cell.Margin.Count, strata.Sum(stratum => stratum.Count));

        // And the extremes really are different tables: the row outnumbered, and the row
        // outnumbering. They need not agree, which is the whole point of splitting them.
        Assert.True(cell.MarginAtComposition(1).Count > 0);
        Assert.True(cell.MarginAtComposition(Small.Seats - 1).Count > 0);
    }

    [Fact]
    public void AFieldRateCarriesTheIntervalItsRatioImplies()
    {
        // 🔥 F7. The scalar rate and the ratio over the per-game series are the same number — a
        // hardening must not move a published figure — but only the series-based one carries a
        // standard error, which is what §12/§13 need to compare a rate across fields.
        var cell = Played.Value.FreeForAll;

        Assert.NotNull(cell.Field);
        Assert.Equal(cell.TurnsPerRound, Measurement.Of(cell.Field!.TurnsPerRound).Mean, 9);
        Assert.Equal(cell.RestrictedRate, Measurement.Of(cell.Field!.LockLive).Mean, 9);
        Assert.Equal(cell.LockBiteRate, Measurement.Of(cell.Field!.LockBite).Mean, 9);

        // The turns-per-round series has one value a settled game and a real interval.
        var turns = Measurement.Of(cell.Field!.TurnsPerRound);

        Assert.Equal(cell.Rounds, turns.Count);
        Assert.True(turns.StandardError > 0);

        // A lock-bite rate is conditional on a lock being live, so its series drops the games in
        // which none was — exactly the conditional-series rule TurnsToWin follows.
        Assert.True(Measurement.Of(cell.Field!.LockBite).Count <= cell.Rounds);
    }

    [Fact]
    public void AMoneyMarginIsThePairedDifferenceInDollars()
    {
        // 🔥 F2. The ladder ranks by win rate, but the game's object is money; the money margin is
        // the same head-to-head pairing in dollars a round, from the series each cell already kept.
        var cell = Played.Value.Pairs[0];

        var margin = Measurement.Paired(
            cell.Player(cell.Row).NetPerRoundByGame, cell.Player(cell.Column).NetPerRoundByGame);

        Assert.Equal(cell.Margin.Count, margin.Count);
        Assert.NotEqual(0, margin.Mean);
    }

    [Fact]
    public void ABootstrapIsADeterministicFunctionOfItsSeedAndBracketsThePoint()
    {
        // 🔥 F6. The bootstrap resamples whole games, so it is reproducible given its seed and its
        // point is the full-sample paired margin — the same number Measurement.Paired reports.
        var cell = Played.Value.Pairs[0];
        var left = cell.Player(cell.Row).NetPerRoundByGame;
        var right = cell.Player(cell.Column).NetPerRoundByGame;

        var once = Bootstrap.PairedMargin(left, right, resamples: 2000, seed: 7);
        var again = Bootstrap.PairedMargin(left, right, resamples: 2000, seed: 7);

        Assert.Equal(once.Lower, again.Lower);
        Assert.Equal(once.Upper, again.Upper);

        Assert.Equal(Measurement.Paired(left, right).Mean, once.Point, 9);
        Assert.True(once.Lower <= once.Point && once.Point <= once.Upper);

        // A different seed is allowed to move the interval a little but not the point.
        var elsewhere = Bootstrap.PairedMargin(left, right, resamples: 2000, seed: 8);

        Assert.Equal(once.Point, elsewhere.Point, 9);
    }

    [Fact]
    public void OnLightTailedDataTheNormalIntervalIsCovered()
    {
        // The coverage check's null: on well-behaved data the normal 95% interval sits inside the
        // bootstrap one, so a flag only fires on the heavy tails money a round actually has.
        var random = new Random(20260826);

        var left = new List<GameValue>();
        var right = new List<GameValue>();

        for (var game = 0; game < 800; game++)
        {
            left.Add(new GameValue(game, random.NextDouble()));
            right.Add(new GameValue(game, random.NextDouble()));
        }

        Assert.True(Bootstrap.PairedMargin(left, right, resamples: 3000, seed: 1).NormalIsCovered);
    }

    [Fact]
    public void TheReplicationReadsAMarginTheSameWayAtTwoSeeds()
    {
        // 🔥 F5. Run against itself and every comparison must reproduce — a byte-identical control
        // that proves the two readings are the same code, so a difference at a real second seed is
        // the world moving, not the estimator.
        IReadOnlyList<Strategy> ladder = [.. new[] { "simple", "greedy", "cautious" }.Select(StrategyCatalog.Resolve)];
        IReadOnlyList<Strategy> dial = [.. new[] { "easy", "hard" }.Select(StrategyCatalog.Resolve)];

        var control = Replication.Run(ladder, dial, seats: 4, gamesPerCell: 96, seedA: 20260819, seedB: 20260819, parallel: false);

        Assert.NotEmpty(control.Rows);
        Assert.True(control.EveryVerdictHolds);
        Assert.True(control.EveryMarginInside);
        Assert.All(control.Rows, row => Assert.Equal(row.A.Mean, row.B.Mean, 12));
        Assert.All(control.Rows, row => Assert.Equal("reproduces", row.Reading));

        // A real second seed produces the same rows with (in general) different numbers; the row
        // set is the ladder matrix plus the dial steps, keyed the way measurements.csv keys them.
        var fresh = Replication.Run(ladder, dial, seats: 4, gamesPerCell: 96, seedA: 20260819, seedB: 20261234, parallel: false);

        Assert.Contains(fresh.Rows, row => row.Id.StartsWith("ladder.head-to-head.", StringComparison.Ordinal));
        Assert.Contains(fresh.Rows, row => row.Id.StartsWith("difficulty.step.", StringComparison.Ordinal));
    }

    [Fact]
    public void TheEfficientReplicationReadsSeedAFromTheFileAndRunsOnlySeedB()
    {
        // 🔥 F5, Nick's call: the published seed-A matrix is read from the file the suite wrote and
        // only seed B is computed — the seed-A side carries the file's mean, interval and verdict,
        // and the seed-B side is a fresh Tournament.Run.
        IReadOnlyList<Strategy> ladder = [.. new[] { "simple", "greedy" }.Select(StrategyCatalog.Resolve)];
        IReadOnlyList<Strategy> dial = [.. new[] { "easy", "hard" }.Select(StrategyCatalog.Resolve)];

        var path = Path.Combine(Path.GetTempPath(), $"burmese-published-{Guid.NewGuid():N}.csv");

        try
        {
            File.WriteAllLines(path,
            [
                "id,subject,metric,games,mean,error,interval,verdict,seed,seats,question,command",
                "ladder.head-to-head.simple-over-greedy,simple vs greedy,win rate margin,8000,"
                + "-0.074000,0.000400,0.000800,separated (Holm),20260819,4,q,c",
                "difficulty.step.hard-over-easy,hard over easy,win rate margin,8000,"
                + "0.150000,0.000800,0.001600,separated (Holm),20260819,4,q,c"
            ]);

            var report = Replication.AgainstPublished(
                path, ladder, dial, seats: 4, gamesPerCell: 96, seedB: 20261234, parallel: false);

            var pair = report.Row("ladder.head-to-head.simple-over-greedy");

            Assert.Equal(-0.074, pair.A.Mean, 6);
            Assert.Equal(0.0008, pair.A.Interval, 6);
            Assert.Equal("separated (Holm)", pair.VerdictA);

            // Seed B is computed, not read: a real count and (in general) a different number.
            Assert.True(pair.B.Count > 0);
            Assert.Contains(report.Rows, row => row.Id == "difficulty.step.hard-over-easy");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AVerdictThatSurvivedAtBothSeedsHoldsAndOneThatFellDoesNot()
    {
        // The reading is a function of the two Holm verdicts and the two intervals, and it is what
        // the CSV's verdict column and the report's prediction are built on — so it is asserted
        // directly rather than only through a run.
        var strong = new Measurement(8000, 0.05, 0.005);
        var weak = new Measurement(8000, 0.001, 0.005);

        Assert.True(new ReplicationRow("x", "x", strong, "separated (Holm)", strong, "separated (Holm)").VerdictHolds);
        Assert.False(new ReplicationRow("x", "x", strong, "separated (Holm)", weak, "inside the interval").VerdictHolds);
        Assert.True(new ReplicationRow("x", "x", weak, "inside the interval", strong, "separated (Holm)").VerdictHolds);

        Assert.True(new ReplicationRow("x", "x", strong, "separated (Holm)", strong, "separated (Holm)").MarginInside);
        Assert.False(new ReplicationRow("x", "x",
            new Measurement(8000, 0.05, 0.005), "separated (Holm)",
            new Measurement(8000, 0.2, 0.005), "separated (Holm)").MarginInside);
    }
}
