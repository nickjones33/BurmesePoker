using BurmesePoker.Domain.Money;

using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// The sweep that asks whether the side bet is worth playing for (BUILD-PLAN P22) — the first
/// experiment in the programme whose answer is not a win rate.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it found is not here</b>; that is <c>docs/strategy/money.csv</c> and
/// <c>docs/STRATEGY.md</c> §10, at eight thousand games a cell. What belongs here is the design:
/// that the stakes really are the only thing varying between two cells, that a cell measures the
/// money rather than the win rate, and that the harness says out loud when the two disagree
/// instead of leaving it to be noticed (P22 acceptance 2).
/// </para>
/// <para>
/// ⚠️ <b>The cells are small on purpose and no assertion here is about a measured value.</b> A
/// test that asserted a margin would be asserting noise; every claim below is about the
/// apparatus.
/// </para>
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class MoneySweepTests
{
    private static MoneySweepOptions Small => new()
    {
        Challenger = StrategyCatalog.Resolve("prospector"),
        Reference = StrategyCatalog.Resolve("outs"),
        Ratios = [new(5, 1), new(5, 20), new(5, 40)],
        Seats = 4,
        GamesPerCell = 14,
        MasterSeed = 20260819,
        Parallel = false
    };

    /// <summary>
    /// One sweep, shared by every test that only reads it.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The rungs here are the two most expensive in the catalog</b> (P21: <c>outs</c> costs
    /// about eight greedy rounds), so a fixture played once a test would put minutes on the
    /// suite for nothing. The one test that needs two runs is the one about determinism, and it
    /// plays them itself.
    /// </remarks>
    private static readonly Lazy<MoneyReport> Once = new(() => MoneySweep.Run(Small));

    [Fact]
    public void EveryRatioIsDealtFromTheSameShoesSoOnlyTheStakesDiffer()
    {
        // 🔥 The design, and the reason the cells share a master seed. Game i is dealt from the
        // same shoe at every ratio, so a difference between two cells is a difference in the
        // money and in nothing else — the same discipline the tournament's paired margin rests
        // on (Measurement.Paired refuses a join across master seeds outright).
        var report = Once.Value;

        var seeds = report.Cells
            .Select(cell => cell.Player("prospector").SeatRounds.Count)
            .Distinct()
            .ToList();

        Assert.Single(seeds);
        Assert.Equal(3, report.Cells.Count);
        Assert.All(report.Cells, cell => Assert.Equal(report.Cells[0].Games, cell.Games));
        Assert.All(report.Cells, cell => Assert.True(cell.Player("prospector").IsSeatBalanced));
        Assert.All(report.Cells, cell => Assert.True(cell.Player("outs").IsSeatBalanced));

        // The stakes are what moved, and they moved in the order they were named.
        Assert.Equal([1, 20, 40], report.Cells.Select(cell => cell.Stakes.MoneyCardValue));
        Assert.Equal([0.2, 4, 8], report.Cells.Select(cell => cell.Ratio));
    }

    [Fact]
    public void AtTheStandardStakesTheTwoRungsPlayOneGameUnderTwoNames()
    {
        // 🔥 The packet's headline, and it is an identity rather than a measurement: at $5/$1
        // one melded card is worth about a dollar and change while a blind draw's ownership is
        // worth pennies, so the money-aware take rule never fires and `prospector` *is* `outs`.
        // ⚠️ It cannot be shown from a head-to-head cell — the two labels sit in different seats
        // there, so their aggregates differ by seat luck however identical the players are. What
        // settles it is two tables of one rung each, dealt from the same shoes.
        Assert.Equal(
            Rounds(Simulator.Run(WholeTableOf("outs", Stakes.Standard))),
            Rounds(Simulator.Run(WholeTableOf("prospector", Stakes.Standard))));

        // And the rule does fire somewhere, or the sweep would be four null cells: by $5/$40 a
        // money card is worth eight rounds, and the same two tables part company.
        Assert.NotEqual(
            Rounds(Simulator.Run(WholeTableOf("outs", new Stakes(5, 40)))),
            Rounds(Simulator.Run(WholeTableOf("prospector", new Stakes(5, 40)))));
    }

    /// <summary>One rung at all four seats, so a difference can only be the rung.</summary>
    private static SimulationOptions WholeTableOf(string rung, Stakes stakes) => new SimulationOptions
    {
        Strategies = [StrategyCatalog.Resolve(rung)],
        Seats = 4,
        Games = 14,
        RoundsPerGame = 1,
        MasterSeed = 20260819,
        Stakes = stakes,
        Parallel = false
    }.Validated();

    /// <summary>
    /// Every round of a run as text, with the strategy's name taken out of it.
    /// </summary>
    /// <remarks>
    /// <b>The name is dropped on purpose</b>: what is being compared is the play, and the name
    /// is the one thing that is certainly different.
    /// </remarks>
    private static IReadOnlyList<string> Rounds(SimulationReport report) =>
    [
        .. report.Games.SelectMany(game => game.Rounds.Select(round =>
            $"{game.Seed}:{round.Turns}:{round.Reshuffles}:"
            + string.Join(
                "|",
                round.Seats.Select(seat =>
                    $"{seat.Seat},{seat.Won},{seat.Covered},{seat.Takes},{seat.Draws},{seat.Claims}"))))
    ];

    [Fact]
    public void TheVerdictIsTheMoneyAndTheWinRateIsReportedBesideIt()
    {
        // ⚠️ P22 acceptance 1 and 2. The margin the family is corrected over is the money one,
        // because a rung that wins fewer rounds and banks more is the better player — and the
        // win rate is carried on the same cell so the two can be seen to come apart rather than
        // one standing in for the other.
        var report = Once.Value;

        Assert.Equal(report.Cells.Count, report.Verdicts.Count);

        for (var index = 0; index < report.Cells.Count; index++)
        {
            Assert.Equal(report.Cells[index].Label, report.Verdicts[index].Label);
            Assert.Equal(report.Cells[index].NetMargin, report.Verdicts[index].Difference);
        }

        // A disagreement is a claim about two separated numbers, never about two noisy ones.
        Assert.All(report.Divergences, cell =>
        {
            Assert.True(cell.NetMargin.IsSeparatedFromZero);
            Assert.True(cell.WinMargin.IsSeparatedFromZero);
            Assert.NotEqual(Math.Sign(cell.NetMargin.Mean), Math.Sign(cell.WinMargin.Mean));
        });
    }

    [Fact]
    public void TwoRunsOfOneSeedWriteTheSameFile()
    {
        // Determinism, at the level the documentation depends on (P22 acceptance 4): the file
        // the docs quote has to be a function of the seed and nothing else, or a figure cannot
        // be re-examined.
        var rows = MoneyCsv.Rows(MoneySweep.Run(Small)).ToList();

        Assert.Equal(rows, MoneyCsv.Rows(Once.Value));

        Assert.Equal(Small.Ratios.Count + 1, rows.Count);
        Assert.All(rows, row => Assert.Equal(rows[0].Count(character => character == ','), row.Count(character => character == ',')));
    }

    [Fact]
    public void ASweepThatCannotAnswerTheQuestionIsRefusedRatherThanRun()
    {
        // The answer is expected to depend on the stakes, so one cell would answer only "not at
        // these stakes" and could not say how far away the answer is (P22 acceptance 1).
        Assert.Throws<ArgumentException>(() => (Small with { Ratios = [Stakes.Standard] }).Validated());

        // Two cells of one ratio measure one thing twice and correct as though they were two.
        Assert.Throws<ArgumentException>(() =>
            (Small with { Ratios = [new(5, 1), new(5, 1), new(5, 40)] }).Validated());

        // And a rung against itself is a null cell wearing a comparison's name.
        Assert.Throws<ArgumentException>(() =>
            (Small with { Challenger = StrategyCatalog.Resolve("outs") }).Validated());
    }
}
