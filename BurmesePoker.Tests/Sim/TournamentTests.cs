using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// The tournament (packet P17): every unordered pair head to head, the free-for-all beside it,
/// and the harness measuring its own bias.
/// </summary>
/// <remarks>
/// <b>The runs here are small and the win rates they produce mean nothing</b> — a ranking worth
/// quoting comes from thousands of games at the command line and lives in
/// <c>docs/strategy/measurements.csv</c>. What a test can check is that the design is the
/// design: that every cell is seat-balanced by construction, that a margin is a per-game
/// difference and not two independent samples, that the family of comparisons is corrected, and
/// that the whole thing is a pure function of its master seed.
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class TournamentTests
{
    private static readonly TournamentOptions Small = new()
    {
        Strategies = [.. new[] { "simple", "greedy", "cautious" }.Select(StrategyCatalog.Resolve)],
        Seats = 4,
        GamesPerCell = 56,
        MasterSeed = 20260819,
        Parallel = false
    };

    private static readonly Lazy<TournamentReport> Played = new(() => Tournament.Run(Small));

    [Fact]
    public void AHeadToHeadCellSeatsBothStrategiesInEveryGameAndEverySeatEquallyOften()
    {
        // Two things at once, and they are the same thing. Balanced() across two strategies
        // includes [A,A,A,A] and [B,B,B,B], in which one of them is not at the table — those
        // games say nothing about A against B *and* they would break the pairing, because a
        // per-game difference needs both sides to have played that game.
        IReadOnlyList<Strategy> pair = [StrategyCatalog.Resolve("simple"), StrategyCatalog.Resolve("greedy")];

        var assignments = SeatingPlan.HeadToHead(pair, 4);

        Assert.Equal((1 << 4) - 2, assignments.Count);
        Assert.All(assignments, assignment => Assert.Equal(2, assignment.Select(s => s.Name).Distinct().Count()));

        foreach (var seat in Enumerable.Range(0, 4))
        {
            Assert.Equal(
                assignments.Count / 2,
                assignments.Count(assignment => assignment[seat].Name == "simple"));
        }
    }

    [Fact]
    public void ACellPlaysAWholeNumberOfPassesThroughItsSeatings()
    {
        // ⚠️ Balance is arithmetic, not luck. A cell whose game count is not a multiple of its
        // seating count has played some seatings once more than others, and nothing downstream
        // can see it — so the wanted number is rounded up rather than warned about.
        Assert.Equal(56, Small.GamesFor(14));
        Assert.Equal(70, (Small with { GamesPerCell = 57 }).GamesFor(14));
        Assert.Equal(256, (Small with { GamesPerCell = 200 }).GamesFor(256));

        foreach (var cell in Played.Value.Pairs)
        {
            Assert.Equal(0, cell.Games % cell.Assignments);
            Assert.All(cell.Players, player => Assert.True(player.IsSeatBalanced));
        }
    }

    [Fact]
    public void EveryPairMeetsAndEveryMarginCarriesAnInterval()
    {
        // P17 acceptance 1: a matrix and a ranking, every cell stating its games and every
        // difference carrying an interval.
        var report = Played.Value;

        Assert.Equal(3, report.Comparisons);
        Assert.Equal(report.Pairs.Count, report.Verdicts.Count);
        Assert.Equal(Small.Strategies.Count, report.Ranking.Count);

        foreach (var cell in report.Pairs)
        {
            Assert.Equal(Small.GamesPerCell, cell.Games);
            Assert.Equal(cell.Settled, cell.Margin.Count);
            Assert.True(cell.Margin.StandardError > 0);
        }

        // Read either way round it is the same comparison with the sign turned over.
        var (forwards, _) = report.Head("simple", "greedy");
        var (backwards, _) = report.Head("greedy", "simple");

        Assert.Equal(forwards.Mean, -backwards.Mean, 12);
        Assert.Equal(forwards.StandardError, backwards.StandardError, 12);
    }

    [Fact]
    public void TheHarnessMeasuresNoDifferenceBetweenAStrategyAndACopyOfItself()
    {
        // 🔥 P17 acceptance 2, and the cheapest test in the packet. Two labels of one strategy
        // differ in nothing whatever, so any gap between them is the apparatus — a seat that
        // opens, a seating that is not balanced, a series that counts seats as trials. It is
        // the test that would have caught P16's seating artifact from the inside.
        var report = Played.Value;

        Assert.Equal(CellKind.Null, report.NullCell.Kind);
        Assert.EndsWith(Tournament.MirrorSuffix, report.NullCell.Column, StringComparison.Ordinal);
        Assert.True(report.NullTestHolds);

        foreach (var player in report.NullCell.Players)
        {
            Assert.True(
                Math.Abs(player.WinRate.Mean - 0.25) <= player.WinRate.Interval,
                $"{player.Name} won {player.WinRate.Mean:0.000} of a table it should share four ways.");

            Assert.True(player.IsSeatBalanced);
        }

        Assert.False(report.NullCell.Margin.IsSeparatedFromZero);
    }

    [Fact]
    public void AMarginIsAPerGameDifferenceAndTheIndependentFormulaUnderstatesIt()
    {
        // 🔥 P17 acceptance 3, corrected by the measurement. Pairing does not mean "narrower":
        // it means the correlation that is really there. Two strategies at *one* table are
        // negatively correlated, because exactly one seat declares — so within a cell the
        // add-the-variances formula is anti-conservative and the paired interval is the honest
        // one. The two means are over the same games either way.
        var report = Played.Value;

        foreach (var cell in report.Pairs)
        {
            Assert.Equal(cell.IndependentMargin.Mean, cell.Margin.Mean, 12);
            Assert.True(
                cell.Margin.StandardError > cell.IndependentMargin.StandardError,
                $"{cell.Label}: paired {cell.Margin.StandardError:0.0000} against "
                + $"independent {cell.IndependentMargin.StandardError:0.0000}");
        }

        Assert.All(
            report.Pairing.Where(check => check.Scope == "within-cell"),
            check => Assert.True(check.Ratio > 1));
    }

    [Fact]
    public void PairingNarrowsAComparisonAcrossTwoCellsOfTheSameMasterSeed()
    {
        // The other half of acceptance 3, and the half the packet was written about: the same
        // strategy at two different tables, dealt from the same shoes, is a paired comparison
        // and pairing it is free variance reduction. ⚠️ Both cells must seat that strategy the
        // same way round, or moving it about the table throws the common shoe away.
        var across = Played.Value.Pairing.Where(check => check.Scope == "across-cells").ToList();

        Assert.NotEmpty(across);
        Assert.All(across, check => Assert.True(
            check.Ratio < 1,
            $"{check.Label}: paired {check.Paired.StandardError:0.0000} against "
            + $"independent {check.Independent.StandardError:0.0000}"));
    }

    [Fact]
    public void ARawSeparationThatDoesNotSurviveTheCorrectionIsShownAsNotSurviving()
    {
        // P17 acceptance 4. The correction is reported beside the raw verdict and never in
        // place of it, so a reader can see what it cost.
        var report = Played.Value;

        Assert.All(report.Verdicts, verdict =>
        {
            Assert.InRange(verdict.Rank, 1, report.Comparisons);
            Assert.Equal(report.Options.Alpha / (report.Comparisons - verdict.Rank + 1), verdict.Threshold, 12);
            Assert.Equal(verdict.Difference.IsSeparatedFromZero, verdict.Separated);
        });

        // Surviving is strictly harder than being separated, in every family there is.
        Assert.All(report.Verdicts, verdict => Assert.True(verdict.Separated || !verdict.Survives));
        Assert.Equal([1, 2, 3], report.Verdicts.Select(verdict => verdict.Rank).Order());
    }

    [Fact]
    public void TheWholeTournamentIsAPureFunctionOfItsMasterSeed()
    {
        // P17 acceptance 5. A ranking that could not be re-run is an opinion, and docs/STRATEGY.md
        // quotes a file this command writes.
        // ⚠️ One pass through the seatings a cell, because this test plays four tournaments
        // and the claim does not get truer with more games. Everything else in the class runs
        // at the shared size.
        var tiny = Small with { GamesPerCell = 1 };

        var serial = Tournament.Run(tiny);
        var again = Tournament.Run(tiny);
        var parallel = Tournament.Run(tiny with { Parallel = true });
        var elsewhere = Tournament.Run(tiny with { MasterSeed = 7 });

        Assert.Equal(TournamentCsv.Rows(serial), TournamentCsv.Rows(again));
        Assert.Equal(TournamentCsv.Rows(serial), TournamentCsv.Rows(parallel));
        Assert.NotEqual(TournamentCsv.Rows(serial), TournamentCsv.Rows(elsewhere));
    }

    [Fact]
    public void TheRankingReadsTheSignOfEveryCellFromTheRightEnd()
    {
        // A cell is stored once, from the row's point of view, and the column reads it
        // backwards. Getting that sign wrong produces a perfectly plausible ranking of the
        // field upside down, which no win rate in the table would contradict.
        var report = Played.Value;

        foreach (var standing in report.Ranking)
        {
            var margins = report.Options.Strategies
                .Where(other => other.Name != standing.Name)
                .Select(other => report.Head(standing.Name, other.Name).Margin.Mean)
                .ToList();

            Assert.Equal(margins.Average(), standing.MeanMargin, 12);
            Assert.Equal(margins.Count, standing.Beaten + standing.LostTo + standing.Undecided);
        }

        // Strongest first, and a tie broken by name so two runs agree.
        Assert.Equal(
            report.Ranking.Select(standing => standing.MeanMargin).OrderByDescending(margin => margin),
            report.Ranking.Select(standing => standing.MeanMargin));
    }

    [Fact]
    public void ARoundRobinOfOneAndTwoStrategiesUnderOneNameAreBothRefused()
    {
        Assert.Throws<ArgumentException>(() =>
            (Small with { Strategies = [StrategyCatalog.Resolve("greedy")] }).Validated());

        Assert.Throws<ArgumentException>(() =>
            (Small with
            {
                Strategies = [StrategyCatalog.Resolve("greedy"), StrategyCatalog.Resolve("greedy")]
            }).Validated());

        Assert.Throws<ArgumentException>(() => (Small with { NullTest = "nobody" }).Validated());
    }
}
