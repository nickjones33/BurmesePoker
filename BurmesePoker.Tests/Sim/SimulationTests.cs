using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// The simulation harness (packet P12): thousands of games, seeded, parallel, and reproducible.
/// </summary>
/// <remarks>
/// <b>The run these tests share is small on purpose</b> — sixteen games, so the suite stays
/// quick — but nothing about it is different in kind from the runs that answer questions. The
/// numbers a strategy comparison is worth reading at come from the command line, not from here.
/// </remarks>
[Collection(WallClockBudgets.Collection)]
public class SimulationTests
{
    private static readonly Strategy Greedy = StrategyCatalog.Resolve("greedy");
    private static readonly Strategy Simple = StrategyCatalog.Resolve("simple");

    private static readonly SimulationOptions Comparison = new()
    {
        Strategies = [Greedy, Simple],
        Seats = 4,
        Games = 16,
        MasterSeed = 20260818
    };

    /// <summary>Played once and shared: every test below asks a different question of it.</summary>
    private static readonly Lazy<SimulationReport> Played = new(() => Simulator.Run(Comparison));

    [Fact]
    public void TheSameMasterSeedRunsIdenticallyInParallelAndInSeries()
    {
        // The test the whole packet stands on. Parallelism must be a scheduling detail: if a
        // game's cards depended on how many games had been dealt before it, a surprising
        // result could never be looked into, only retold.
        var parallel = CsvReport.Rows(Played.Value).ToList();
        var serial = CsvReport.Rows(Simulator.Run(Comparison with { Parallel = false })).ToList();
        var again = CsvReport.Rows(Simulator.Run(Comparison with { MaxDegreeOfParallelism = 2 })).ToList();

        Assert.Equal(serial, parallel);
        Assert.Equal(serial, again);
        Assert.Equal(Comparison.Games * Comparison.Seats + 1, serial.Count);
    }

    [Fact]
    public void ADifferentMasterSeedIsADifferentRun()
    {
        // The other half of the same claim: identical output must mean the seed, not a
        // harness that quietly ignores it.
        var elsewhere = CsvReport.Rows(Simulator.Run(Comparison with { MasterSeed = 7 })).ToList();

        Assert.NotEqual(CsvReport.Rows(Played.Value).ToList(), elsewhere);
    }

    [Fact]
    public void EveryGameIsReplayableOnItsOwnFromTheSeedItRecorded()
    {
        // A row's join keys have to be enough to get the game back (BUILD-PLAN §3.8 item 4).
        var report = Played.Value;

        foreach (var game in report.Games)
        {
            var replayed = GameRunner.Play(Comparison, game.Game);

            Assert.Equal(game.Seed, replayed.Seed);
            Assert.Equal(game.Seating, replayed.Seating);
            Assert.Equal(Winners(game), Winners(replayed));
            Assert.Equal(Nets(game), Nets(replayed));
        }

        // And the seeds really are per game, rather than one seed used sixteen times.
        Assert.Equal(report.Games.Count, report.Games.Select(game => game.Seed).Distinct().Count());
    }

    [Fact]
    public void MoneyIsConservedInEveryRoundOfEverySimulatedGame()
    {
        // Not on average, and not per game: every round settles to zero, and the two halves
        // the harness reports always add back up to what the domain actually paid out.
        var rounds = Played.Value.Games.SelectMany(game => game.Rounds).ToList();

        Assert.NotEmpty(rounds);

        foreach (var round in rounds)
        {
            Assert.Equal(0, round.Seats.Sum(seat => seat.Net));
            Assert.Equal(0, round.Seats.Sum(seat => seat.Flat));
            Assert.Equal(0, round.Seats.Sum(seat => seat.SideBet));
            Assert.All(round.Seats, seat => Assert.Equal(seat.Net, seat.Flat + seat.SideBet));
            Assert.Equal(1, round.Seats.Count(seat => seat.Won));
        }

        // And the totals the report adds up conserve it too.
        Assert.Equal(0, Played.Value.Strategies.Sum(strategy => strategy.Net));
        Assert.Equal(0, Played.Value.Strategies.Sum(strategy => strategy.SideBet));
    }

    [Fact]
    public void NeitherTheDomainNorTheHarnessHoldsMutableStaticState()
    {
        // Pinned deliberately (BUILD-PLAN §3.7 item 3), because a convenience cache added
        // years from now would break parallel play silently rather than loudly: games would
        // start interfering, and only the numbers would show it.
        //
        // It pins the reference, not the graph behind it — a `static readonly Card[]` is
        // accepted here and would still be shared. What it catches is the field somebody
        // means to assign to.
        var mutable =
            from assembly in new[] { typeof(Card).Assembly, typeof(Simulator).Assembly }
            from type in assembly.GetTypes()
            where !IsCompilerGenerated(type)
            from field in type.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            where !field.IsInitOnly && !field.IsLiteral
            select $"{type.FullName}.{field.Name}";

        Assert.Empty(mutable);
    }

    [Fact]
    public void AStalledRoundIsAbandonedAtTheTurnCapAndSaidSo()
    {
        // Only a declaration ends a round (RULES.md §7.1) and the reshuffle keeps the cards
        // moving, so a table that never improves plays for ever. The domain will not invent a
        // limit — that would be inventing a rule — so the harness bounds the round itself and
        // reports it, because a strategy that stalls is a result and not a crash.
        var stalling = Simulator.Run(new SimulationOptions
        {
            Strategies = [new Strategy("stalls", _ => new StallingAgent())],
            Games = 3,
            TurnCap = 40,
            MasterSeed = 5
        });

        Assert.Equal(3, stalling.AbandonedGames);
        Assert.Equal(0, stalling.Rounds);
        Assert.All(stalling.Games, game => Assert.True(game.Abandoned));
        Assert.All(stalling.Games, game => Assert.Empty(game.Rounds));
    }

    [Fact]
    public void TheStrategiesAreRotatedThroughTheSeats()
    {
        // Seat 0 opens every round and is the only seat ever offered the turned-up money card
        // (RULES.md §4.5), so a comparison that never moved a strategy out of it would be
        // measuring the seat.
        var report = Played.Value;

        Assert.Equal(["greedy", "simple", "greedy", "simple"], report.Games[0].Seating);
        Assert.Equal(["simple", "greedy", "simple", "greedy"], report.Games[1].Seating);

        var openingSeats = report.Games.Select(game => game.Seating[0]).Distinct().Order();
        Assert.Equal(["greedy", "simple"], openingSeats);
    }

    [Fact]
    public void OnlyTheOpeningSeatIsEverOfferedTheTurnedUpMoneyCard()
    {
        // Read off the seat decorator rather than the observer, because only the agent knows
        // it was asked: an offer that is declined is narrated to nobody (BUILD-PLAN §3.8 item 2).
        foreach (var round in Played.Value.Games.SelectMany(game => game.Rounds))
        {
            Assert.Equal(1, round.Seats[0].ClaimOffers);
            Assert.All(round.Seats.Skip(1), seat => Assert.Equal(0, seat.ClaimOffers));
            Assert.All(round.Seats, seat => Assert.True(seat.Claims <= seat.ClaimOffers));
        }
    }

    [Fact]
    public void TheTieBreakIsWorthWinningWith()
    {
        // P10 claimed the discard tie-break — keep cards with partners, keep jokers — is what
        // makes progress, since early on almost every discard scores alike. `SimpleBotAgent`
        // is that bot with the tie-break taken out and nothing else changed, so this is the
        // claim measured rather than asserted.
        var report = Played.Value;
        var greedy = report.Strategies.Single(strategy => strategy.Name == "greedy");
        var simple = report.Strategies.Single(strategy => strategy.Name == "simple");

        Assert.Equal(report.Games.Count * 2, greedy.Rounds);
        Assert.Equal(greedy.Rounds, simple.Rounds);
        Assert.True(greedy.Wins > simple.Wins, $"greedy won {greedy.Wins}, simple won {simple.Wins}.");
        Assert.True(greedy.Net > simple.Net, $"greedy banked {greedy.Net}, simple banked {simple.Net}.");
        Assert.Equal(-greedy.Net, simple.Net);
    }

    [Fact]
    public void EveryRowCarriesTheKeysThatMakeItAttributable()
    {
        var rows = CsvReport.Rows(Played.Value).ToList();

        Assert.StartsWith("master_seed,game,game_seed,round,seat,strategy,", rows[0]);

        var first = rows[1].Split(',');
        Assert.Equal("20260818", first[0]);
        Assert.Equal("0", first[1]);
        Assert.Equal(Played.Value.Games[0].Seed.ToString(), first[2]);
        Assert.Equal("greedy", first[5]);
    }

    [Fact]
    public void EveryRowAlsoNamesWhoFedItAndWhoItFed()
    {
        // P16's join keys. Both columns are pure functions of the seating the row's game
        // already carries, and both are derived from *the seating* rather than from the run's
        // options — which is what lets a replayed journal, whose options are reconstructed from
        // headers, produce the same two columns (P14).
        var report = Played.Value;
        var rows = CsvReport.Rows(report).Skip(1).Select(row => row.Split(',')).ToList();

        Assert.Contains("upstream_strategy,downstream_strategy", CsvReport.Rows(report).First());

        foreach (var row in rows)
        {
            var game = report.Games[int.Parse(row[1], CultureInfo.InvariantCulture)];
            var seat = int.Parse(row[4], CultureInfo.InvariantCulture);
            var seats = game.Seating.Count;

            Assert.Equal(game.Seating[seat], row[5]);
            Assert.Equal(game.Seating[(seat - 1 + seats) % seats], row[6]);
            Assert.Equal(game.Seating[(seat + 1) % seats], row[7]);
        }

        // ⚠️ The wrap-around is the whole point: a table is a cycle, so the seat that feeds
        // seat 0 is the last one, and getting that backwards would be invisible in a win rate.
        var opener = rows.First(row => row[4] == "0");
        var closer = report.Games[int.Parse(opener[1], CultureInfo.InvariantCulture)].Seating[^1];

        Assert.Equal(closer, opener[6]);
    }

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        || (type.DeclaringType is { } outer && IsCompilerGenerated(outer));

    private static IEnumerable<int> Winners(GameResult game) =>
        game.Rounds.SelectMany(round => round.Seats.Where(seat => seat.Won).Select(seat => seat.Seat));

    private static IEnumerable<int> Nets(GameResult game) =>
        game.Rounds.SelectMany(round => round.Seats.Select(seat => seat.Net));

    /// <summary>Throws back whatever it just took, so its hand never changes and it never wins.</summary>
    private sealed class StallingAgent : IPlayerAgent
    {
        public TurnAction ChooseAction(TurnContext context) => TurnAction.DrawFromDeck;

        public Card ChooseDiscard(TurnContext context) => context.Taken ?? context.Hand[^1];

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => false;

        public bool ObjectToClaim(TurnContext context) => false;

        public bool Declare(TurnContext context) => true;
    }
}
