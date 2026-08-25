using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// The ladder through the harness (packet P15): four rungs that must each be reproducible,
/// each be bounded, and between them cover a real range of skill.
/// </summary>
/// <remarks>
/// <b>The runs here are small on purpose.</b> A win rate worth quoting comes from thousands of
/// games at the command line and is reported with an interval; what these can check is that
/// the rungs are wired up, that a seed still means what it says now that a strategy draws
/// random numbers, and that the ordering is not an accident of one seed.
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class SkillLadderRunTests
{
    private static readonly SimulationOptions Ladder = new()
    {
        Strategies = [.. new[] { "random", "simple", "greedy", "cautious" }.Select(StrategyCatalog.Resolve)],
        Seats = 4,
        // ⚠️ Eight was enough until §5.1 (P27). The feeding ban costs `simple` — the rung with no
        // tie-break — enough rounds that it drew a blank across eight games at this seed, and an
        // ordering test that can be beaten by one unlucky run is not testing an ordering. At 200
        // games the rungs measure 0% / 29.0% / 43.0% / 28.0%, so thirty-two is a margin rather
        // than a hope, and the run is still under a second.
        Games = 32,
        MasterSeed = 20260819,

        // ⚠️ Serial by default, and the determinism test is the one that asks for threads.
        // A test class that fans every run across every core starves whatever else the suite
        // is running — and two of the tests in this suite are wall-clock budgets, which is a
        // documented way to make a green tree look red (`STATUS.md`).
        Parallel = false
    };

    /// <summary>Played once and shared, exactly as <see cref="SimulationTests"/> shares its own.</summary>
    private static readonly Lazy<SimulationReport> Played = new(() => Simulator.Run(Ladder));

    [Fact]
    public void TheWholeLadderIsStillAPureFunctionOfItsMasterSeed()
    {
        // P15 acceptance 2. A run is only comparable to another run because it is
        // reproducible, and the random rung is where that is easiest to lose — so the whole
        // suite of determinism claims P12 made is re-run with one at the table.
        var serial = CsvReport.Rows(Played.Value).ToList();
        var parallel = CsvReport.Rows(Simulator.Run(Ladder with { Parallel = true })).ToList();
        var elsewhere = CsvReport.Rows(Simulator.Run(Ladder with { MasterSeed = 7 })).ToList();

        Assert.Equal(serial, parallel);
        Assert.NotEqual(serial, elsewhere);
    }

    [Fact]
    public void TwoRandomSeatsAtOneTableDoNotPlayInLockstep()
    {
        // The seat seed is derived from the game's *and* the seat's, so a table of random
        // bots is four different players rather than one played four times. Two seats drawing
        // the same numbers would make the floor of the ladder a much stranger thing than
        // "nobody is thinking" — and it is the journal that can say, because a table of pure
        // chance rarely reaches a declaration and so rarely produces a row.
        var chance = Simulator.Run(new SimulationOptions
        {
            Strategies = [StrategyCatalog.Resolve("random")],
            Seats = 4,
            Games = 1,
            TurnCap = 25,
            MasterSeed = 3,
            Parallel = false,
            Journal = JournalFidelity.Thin
        });

        var decisions = chance.Games[0].Journal!.Decisions;
        var answers = chance.Games[0].Journal!.Header.Players
            .Select(player => string.Join(
                " ",
                decisions.Where(decision => decision.Player == player).Select(decision => decision.Answer)))
            .ToList();

        Assert.All(answers, answer => Assert.NotEmpty(answer));
        Assert.Equal(4, answers.Distinct().Count());

        // And the seeds those came from really are four, derived rather than shared.
        var seeds = Enumerable.Range(0, 4).Select(seat => SeedSequence.SeatSeed(chance.Games[0].Seed, seat));
        Assert.Equal(4, seeds.Distinct().Count());
    }

    [Fact]
    public void ATableOfNothingButChanceIsAbandonedRatherThanHung()
    {
        // P15 acceptance 3. The random rung is the first with no monotone score, so it can
        // genuinely stall — and the harness's answer to a stall is to give up on the round
        // and say so, never to drop it (P12).
        var chance = Simulator.Run(new SimulationOptions
        {
            Strategies = [StrategyCatalog.Resolve("random")],
            Seats = 4,
            Games = 2,
            TurnCap = 30,
            MasterSeed = 11,
            Parallel = false
        });

        Assert.Equal(2, chance.Games.Count);
        Assert.All(chance.Games, game => Assert.True(game.Abandoned || game.Rounds.Count == 1));
        Assert.True(chance.AbandonedGames > 0, "not one round of pure chance ran out of turns.");
    }

    [Theory]
    [InlineData("random")]
    [InlineData("simple")]
    [InlineData("greedy")]
    [InlineData("cautious")]
    [InlineData("outs")]
    public void EveryRungJournalsAndReplaysToTheSameRows(string rung)
    {
        // P15 acceptance 5, which P14 made a one-liner. What it catches is the one thing a
        // new strategy can break here: a seat that answered a question the four IPlayerAgent
        // methods do not cover could not be journalled at all, and this is where that would
        // be found rather than in P16's analysis.
        var run = new SimulationOptions
        {
            Strategies = [StrategyCatalog.Resolve(rung), StrategyCatalog.Resolve("greedy")],
            Seats = 4,
            Games = 2,
            TurnCap = 60,
            MasterSeed = 20260819,
            Parallel = false,
            Journal = JournalFidelity.Thin
        };

        var played = Simulator.Run(run);
        var journals = played.Games.Select(game => game.Journal!).ToList();

        Assert.Equal(CsvReport.Rows(played).ToList(), CsvReport.Rows(Replay.Run(journals)).ToList());
    }

    [Fact]
    public void ThinkingBeatsNotThinkingByAMile()
    {
        // P15 acceptance 1 is a *measurement*, reported with an interval from thousands of
        // games — see the packet's own numbers. What is worth pinning in a test is only the
        // part no run of this size could get wrong: a rung that thinks about the cover count
        // wins rounds and one that does not wins almost none. The separations further up the
        // ladder are far too fine for a run this size, and two of them turned out to be finer
        // than they look — which is exactly why they are measured and not asserted.
        var report = Played.Value;

        var wins = report.Strategies.ToDictionary(strategy => strategy.Name, strategy => strategy.Wins);

        Assert.True(wins["random"] < wins["simple"], $"random won {wins["random"]}, simple {wins["simple"]}.");
        Assert.True(wins["random"] < wins["greedy"], $"random won {wins["random"]}, greedy {wins["greedy"]}.");
        Assert.True(wins["random"] < wins["cautious"], $"random won {wins["random"]}, cautious {wins["cautious"]}.");

    }

    [Fact]
    public void EveryRungTheCommandLineOffersCanActuallyBeSeated()
    {
        // The catalog is the command line's whole vocabulary, so a rung that is named but
        // cannot be built is a crash at the CLI rather than a compile error.
        Assert.Equal(
            ["random", "simple", "greedy", "cautious", "counting", "outs", "warden", "opportunist", "prospector", "purist", "angler"],
            StrategyCatalog.All.Select(strategy => strategy.Name));

        Assert.All(StrategyCatalog.All, strategy => Assert.NotNull(strategy.Create(1)));
    }
}
