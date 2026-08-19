using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// The neighbour experiment (packet P16): a focal seat, a dial in the seat before it, and the
/// same dial in the seat after it.
/// </summary>
/// <remarks>
/// <b>The runs here are tiny and the numbers they produce mean nothing</b> — the answer comes
/// from thousands of games at the command line and is quoted with an interval. What a test can
/// check is that the design is the design: that the varied seat really is upstream, that the
/// table's composition is identical in both arms, that the cells are balanced by construction,
/// and that the whole thing is a pure function of its master seed.
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class NeighbourExperimentTests
{
    private static readonly NeighbourOptions Small = new()
    {
        Focal = StrategyCatalog.Resolve("greedy"),
        Levels = [.. new[] { "simple", "greedy", "cautious" }.Select(StrategyCatalog.Resolve)],
        Filler = StrategyCatalog.Resolve("simple"),
        Reference = StrategyCatalog.Resolve("greedy"),
        Seats = 4,
        GamesPerCell = 8,
        MasterSeed = 20260819,
        Parallel = false
    };

    private static readonly Lazy<NeighbourReport> Played = new(() => NeighbourExperiment.Run(Small));

    [Fact]
    public void TheVariedSeatReallyIsTheOneThatFeedsTheFocalSeat()
    {
        // The whole packet is a claim about one edge of a directed cycle, so the direction is
        // worth pinning rather than trusting to an index. Turn order is seat order and the
        // table wraps, so the player before seat 0 is the one in the last seat — the exact
        // wrap-around that is easy to write backwards and impossible to see in a win rate.
        var level = StrategyCatalog.Resolve("random");

        var upstream = NeighbourExperiment.Pattern(Small, Neighbour.Upstream, level);
        var downstream = NeighbourExperiment.Pattern(Small, Neighbour.Downstream, level);

        Assert.Equal(["greedy", "simple", "simple", "random"], upstream.Select(s => s.Name));
        Assert.Equal(["greedy", "random", "simple", "simple"], downstream.Select(s => s.Name));
    }

    [Fact]
    public void BothArmsSeatTheSameTableAndDifferOnlyInWhichWayTheDiscardsFlow()
    {
        // ⚠️ This is the control, and it is the most important line in the packet. If the two
        // arms seated different mixes of strategy, a difference between them would be "strong
        // tables win more" and would say nothing about neighbours at all.
        foreach (var level in Small.Levels)
        {
            var upstream = NeighbourExperiment.Pattern(Small, Neighbour.Upstream, level).Select(s => s.Name).Order();
            var downstream = NeighbourExperiment.Pattern(Small, Neighbour.Downstream, level).Select(s => s.Name).Order();

            Assert.Equal(upstream, downstream);
        }
    }

    [Fact]
    public void WhenTheLevelIsTheFillerTheTwoArmsAreLiterallyTheSameTable()
    {
        // A consistency check the design hands over for free: at level == filler there is no
        // varied seat to move, so the two cells are the same four strategies in the same order
        // played from the same seed, and every number in them must agree to the last digit.
        // If they ever differ, something other than the seating is varying between arms.
        var report = Played.Value;

        var upstream = report.Cell(Neighbour.Upstream, "simple");
        var downstream = report.Cell(Neighbour.Downstream, "simple");

        Assert.Equal(upstream.WinRate, downstream.WinRate);
        Assert.Equal(upstream.TakeRate, downstream.TakeRate);
        Assert.Equal(upstream.NetPerRound, downstream.NetPerRound);
    }

    [Fact]
    public void EveryCellIsPlayedAndTheFocalSeatSitsInEverySeatEquallyOften()
    {
        // P16 acceptance 2: the confound is gone because every cell was *played*, in balance —
        // not because a larger run was assumed to have covered it. Seat 0 opens and is the only
        // seat offered the turned-up money card (P12), so it is a confounder in its own right;
        // cycling the rotations removes it by arithmetic.
        var report = Played.Value;

        Assert.Equal(Small.Levels.Count * 2, report.Cells.Count);

        foreach (var arm in (Neighbour[])[Neighbour.Upstream, Neighbour.Downstream])
        {
            foreach (var level in Small.Levels)
            {
                var cell = report.Cell(arm, level.Name);

                Assert.Equal(Small.GamesPerCell, cell.Games);
                Assert.Equal(Small.GamesPerCell, cell.Settled);
                Assert.Equal(0, cell.Abandoned);
                Assert.Equal([2, 2, 2, 2], cell.FocalSeatGames);
                Assert.Equal(cell.Settled, cell.WinRate.Count);
            }
        }
    }

    [Fact]
    public void TheFocalSeatIsWhereTheAnalysisThinksItIs()
    {
        // The measurement reads one seat out of each game by arithmetic — the games cycle the
        // rotations in order, so the focal seat is the game index modulo the table. That is an
        // agreement between two files, and getting it wrong would produce a full set of
        // plausible numbers about somebody else entirely, so the run itself checks it.
        foreach (var arm in (Neighbour[])[Neighbour.Upstream, Neighbour.Downstream])
        {
            foreach (var level in Small.Levels)
            {
                var run = NeighbourExperiment.RunOf(Small, arm, level);
                var offset = NeighbourExperiment.VariedOffset(Small, arm);

                for (var game = 0; game < Small.GamesPerCell; game++)
                {
                    var seating = run.Seating(game);
                    var focal = game % Small.Seats;

                    Assert.Equal(Small.Focal.Name, seating[focal].Name);
                    Assert.Equal(level.Name, seating[(focal + offset) % Small.Seats].Name);
                }
            }
        }
    }

    [Fact]
    public void TheWholeExperimentIsAPureFunctionOfItsMasterSeed()
    {
        // P16 acceptance 5. An experiment that could not be re-run is an anecdote, and the
        // report names the seed it came from precisely so the claim can be checked later.
        var again = NeighbourExperiment.Run(Small);
        var elsewhere = NeighbourExperiment.Run(Small with { MasterSeed = 7 });

        Assert.Equal(NeighbourCsv.Rows(Played.Value), NeighbourCsv.Rows(again));
        Assert.NotEqual(NeighbourCsv.Rows(Played.Value), NeighbourCsv.Rows(elsewhere));

        // And parallelism stays a scheduling detail, as it is everywhere else in the harness.
        Assert.Equal(
            NeighbourCsv.Rows(Played.Value),
            NeighbourCsv.Rows(NeighbourExperiment.Run(Small with { Parallel = true })));
    }

    [Fact]
    public void AnEffectIsTheDifferenceBetweenTwoCellsAndTheDirectionalOneCancelsTheTable()
    {
        // The arithmetic the verdict rests on, checked against the cells it is derived from
        // rather than reimplemented: a report that quietly reported a cell as an effect would
        // read perfectly plausibly.
        var report = Played.Value;

        var effect = report.Effects.Single(e => e.Arm == Neighbour.Upstream && e.Level == "cautious");
        var cell = report.Cell(Neighbour.Upstream, "cautious");
        var reference = report.Cell(Neighbour.Upstream, "greedy");

        Assert.Equal(cell.WinRate.Mean - reference.WinRate.Mean, effect.WinRate.Mean, 12);
        Assert.DoesNotContain(report.Effects, e => e.Level == "greedy");

        var down = report.Effects.Single(e => e.Arm == Neighbour.Downstream && e.Level == "cautious");
        var (directional, _) = report.Directional("cautious");

        Assert.Equal(effect.WinRate.Mean - down.WinRate.Mean, directional.Mean, 12);
    }

    [Fact]
    public void ADialWithOneSettingAndAReferenceThatIsNotOnItAreBothRefused()
    {
        Assert.Throws<ArgumentException>(() =>
            (Small with { Levels = [StrategyCatalog.Resolve("greedy")] }).Validated());

        Assert.Throws<ArgumentException>(() =>
            (Small with { Reference = StrategyCatalog.Resolve("random") }).Validated());
    }

    [Fact]
    public void AMeasurementCarriesTheIntervalOfAMeanOverGamesRatherThanOverTurns()
    {
        // The unit of independence is the game (see Measurement). Ten coin flips of a fair
        // coin have a standard error near 0.158, and a sample with no spread at all has none.
        var flips = Measurement.Of([1, 0, 1, 0, 1, 0, 1, 0, 1, 0]);

        Assert.Equal(10, flips.Count);
        Assert.Equal(0.5, flips.Mean, 12);
        Assert.Equal(0.1667, flips.StandardError, 3);
        Assert.True(flips.IsSeparatedFromZero);

        var nothing = Measurement.Of([0.01, -0.01, 0.02, -0.02]);

        Assert.False(nothing.IsSeparatedFromZero);

        // A difference adds the variances, which is conservative here because the two cells
        // share a master seed and so are really paired.
        var difference = Measurement.Difference(new Measurement(10, 0.4, 0.3), new Measurement(10, 0.1, 0.4));

        Assert.Equal(0.3, difference.Mean, 12);
        Assert.Equal(0.5, difference.StandardError, 12);
    }
}
