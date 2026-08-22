using BurmesePoker.Domain.Money;

using BurmesePoker.Sim;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// 🔥 <b>P31's mechanism variable: how often RULES.md §5.1 actually took the card a seat meant to
/// throw.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>It is what separates "the rule did nothing" from "the rung did nothing"</b> (BUILD-PLAN
/// P31 item 3). A lock that closes a rank the seat was never going to throw costs it nothing, so a
/// count of <em>closed ranks</em> would report a rule that bites hard and a rule that does not
/// exist as the same number. The counterfactual — what the seat would have thrown over its whole
/// hand — is the only thing that tells them apart, and only the player can answer it.
/// </para>
/// <para>
/// ⚠️ <b>An instrument has to be free of what it measures.</b> Asking the counterfactual costs a
/// second ranking and must change no card, or every figure in a cell that asked for it would be
/// incomparable with every figure in a cell that did not. That is the first test here and it is
/// the load-bearing one.
/// </para>
/// </remarks>
public class LockBiteTests
{
    /// <summary>The whole ladder at one table, which is where the suite buys the variable.</summary>
    private static SimulationOptions Field { get; } = new SimulationOptions
    {
        Strategies = StrategyCatalog.Ladder,
        Seats = 4,
        Games = 32,
        RoundsPerGame = 1,
        MasterSeed = 20260819,
        Stakes = Stakes.Standard,
        Parallel = false
    }.Validated();

    /// <remarks>
    /// ⚠️ <b>The counterfactual is read and thrown away.</b> Every card discarded, every hand,
    /// every payout and every turn count is identical with the instrument on and off — which is
    /// what makes a cell that measured the bite comparable with the ninety rows that did not.
    /// </remarks>
    [Fact]
    public void AskingWhatTheBanTookChangesNothingAboutThePlay()
    {
        Assert.Equal(
            CsvReport.Rows(Simulator.Run(Field)).ToList(),
            CsvReport.Rows(Simulator.Run(Field with { CountLockBites = true })).ToList());
    }

    /// <remarks>
    /// ⚠️ <b>Off is off, and it reads as zero rather than as unknown.</b> The restricted turns are
    /// still counted — they cost nothing, being a list length the turn already computed — so a cell
    /// that did not buy the counterfactual still publishes its denominator.
    /// </remarks>
    [Fact]
    public void TheCounterfactualIsPaidForOnlyWhenACellAsksForIt()
    {
        var quiet = Seats(Simulator.Run(Field));

        Assert.All(quiet, seat => Assert.Equal(0, seat.LockBites));
        Assert.Contains(quiet, seat => seat.RestrictedTurns > 0);
        Assert.All(quiet, seat => Assert.True(seat.RestrictedTurns <= seat.DiscardsChosen));
    }

    /// <remarks>
    /// 🔥 <b>The rule bites, and a null from this rung would therefore be a fact about the rung.</b>
    /// The claim is only that the count is not structurally zero — what it comes to over eight
    /// thousand games a cell is <c>docs/STRATEGY.md</c>'s to say, and it is published either way.
    /// </remarks>
    [Fact]
    public void TheBanDoesTakeCardsSeatsMeantToThrow()
    {
        var counted = Seats(Simulator.Run(Field with { CountLockBites = true }));

        Assert.True(counted.Sum(seat => seat.LockBites) > 0);
        Assert.All(counted, seat => Assert.True(seat.LockBites <= seat.RestrictedTurns));
    }

    private static IReadOnlyList<SeatRow> Seats(SimulationReport report) =>
        [.. report.Games.SelectMany(game => game.Rounds).SelectMany(round => round.Seats)];
}
