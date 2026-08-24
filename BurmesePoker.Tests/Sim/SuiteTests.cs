using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Money;

using BurmesePoker.Sim;

using BurmesePoker.Tests;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// A value per game (packet P17), and the standing set of measurements the documentation quotes.
/// </summary>
[Collection(WallClockBudgets.Collection)]
public class SuiteTests
{
    private static readonly SimulationOptions Headline = new()
    {
        Strategies = [.. new[] { "greedy", "simple" }.Select(StrategyCatalog.Resolve)],
        Seats = 4,
        Games = 24,
        MasterSeed = 20260819,
        Parallel = false
    };

    [Fact]
    public void AStrategyThatHoldsTwoSeatsInAGameContributesOneValueAndNotTwo()
    {
        // 🔥 The unit of independence is the game. A seat's rounds are as correlated as the
        // shoe they were dealt from, so counting seats as trials would tighten every interval
        // by a factor no number of games earns back — the error that turns "no effect" into a
        // finding, which is the one this project has already had to correct once (P15/P16).
        var report = Simulator.Run(Headline);
        var series = StrategySeries.Of(report);

        Assert.All(series, one => Assert.Equal(Headline.Games, Measurement.Of(one.WinRate).Count));
        Assert.All(series, one => Assert.Equal(Headline.Games * 2, one.SeatRounds.Sum()));

        // ⚠️ And each of those values is a total out of its trials, never a ratio — two seats
        // in a game are two trials of one observation, which is what keeps the estimate the
        // same number every earlier packet published.
        Assert.All(series, one => Assert.All(one.WinRate, value => Assert.Equal(2, value.Trials)));

        // And every value is labelled with the seed its game was dealt from, in game order —
        // which is what makes a comparison across two cells a pairing rather than a guess.
        Assert.Equal(
            report.Games.Select(game => game.Seed),
            series[0].WinRate.Select(value => value.GameSeed));
    }

    [Fact]
    public void AMeasuredWinRateIsTheSameNumberTheHarnessHasAlwaysPublished()
    {
        // 🔥 The point of the ratio estimator. A strategy holds a different number of seats in
        // different games of a fully crossed run, so the unweighted average of the per-game
        // ratios is *not* the totals divided — it flatters the stronger strategy by about a
        // point at four seats, which is the size of P16's whole seating effect. Adding an
        // interval to a figure must not change the figure (§3.8 item 4), and this is the test
        // that says so — under a crossed seating, where the two readings can differ.
        IReadOnlyList<Strategy> pair = [.. new[] { "greedy", "simple" }.Select(StrategyCatalog.Resolve)];

        foreach (var options in new[]
        {
            Headline,
            Headline with { Games = 32, Assignments = SeatingPlan.Balanced(pair, 4) }
        })
        {
            var report = Simulator.Run(options);
            var series = StrategySeries.Of(report).ToDictionary(one => one.Name);

            foreach (var summary in report.Strategies)
            {
                Assert.Equal(summary.WinRate, Measurement.Of(series[summary.Name].WinRate).Mean, 12);
                Assert.Equal(summary.NetPerRound, Measurement.Of(series[summary.Name].NetPerRound).Mean, 12);
                Assert.Equal(summary.TakeRate, Measurement.Of(series[summary.Name].TakeRate).Mean, 12);
                Assert.Equal(summary.TurnsToWin, Measurement.Of(series[summary.Name].TurnsToWin).Mean, 12);
            }
        }
    }

    [Fact]
    public void AConditionalSeriesDropsTheGamesItHasNothingToSayAbout()
    {
        // Turns-to-win exists only for a game a strategy won and covered-when-losing only for
        // one it lost. Padding either with zeroes would measure something else entirely and
        // report it with a confident interval.
        var series = StrategySeries.Of(Simulator.Run(Headline));

        foreach (var one in series)
        {
            Assert.True(Measurement.Of(one.TurnsToWin).Count < Measurement.Of(one.WinRate).Count);
            Assert.True(Measurement.Of(one.CoveredWhenLosing).Count <= Measurement.Of(one.WinRate).Count);
            Assert.Contains(one.TurnsToWin, value => value.Trials == 0);

            // A game it never won offers no trials, and a game with no trials is dropped rather
            // than counted as a zero — which would put a confident interval on a fiction.
            Assert.All(
                one.TurnsToWin.Where(value => value.Trials > 0),
                value => Assert.True(value.Total > 0));
        }
    }

    [Fact]
    public void TheSuiteWritesEveryNumberWithTheCommandThatMadeIt()
    {
        // 🔥 §3.12's rule, made mechanical. RULES.md tags every rule with its provenance
        // because a rule without a source cannot be re-examined; a measurement without its
        // origin is worse, because it looks like a fact.
        var report = Suite.Run(new SuiteOptions
        {
            Strategies = [.. new[] { "simple", "greedy" }.Select(StrategyCatalog.Resolve)],
            // ⚠️ Three ratios rather than the shipped four, and only because the sweep's rungs
            // are the expensive ones: what is under test here is that the rows carry their
            // command, not what the sweep found (BUILD-PLAN P22).
            Ratios = [new(5, 1), new(5, 20), new(5, 40)],
            Seats = 4,
            GamesPerCell = 32,
            MasterSeed = 20260819,
            Parallel = false
        });

        Assert.NotEmpty(report.Measurements);
        Assert.All(report.Measurements, measurement =>
        {
            Assert.NotEmpty(measurement.Command);
            Assert.NotEmpty(measurement.Question);
            Assert.Contains("--seed 20260819", measurement.Command, StringComparison.Ordinal);
        });

        var ids = report.Measurements.Select(measurement => measurement.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());

        // The standing set is what the docs actually claim, and P12's headline is in it under
        // both seatings because that is the one figure this project has had to correct.
        Assert.Contains(ids, id => id.StartsWith("harness.null-test.", StringComparison.Ordinal));
        Assert.Contains(ids, id => id.StartsWith("headline.rotate.", StringComparison.Ordinal));
        Assert.Contains(ids, id => id.StartsWith("headline.balanced.", StringComparison.Ordinal));
        Assert.Contains(ids, id => id.StartsWith("ladder.rank.", StringComparison.Ordinal));

        // ⚠️ And P22's rows, which are the only ones in the file not played at the standard
        // stakes: a published money figure is meaningless without the ratio it was played at,
        // so the ratio is in the id rather than in the prose beside it — and the challenger is
        // too, since P44, because there is more than one (one sweep each, every rung ranked on
        // money the subject of its own rows).
        foreach (var challenger in BotCatalog.StakesSensitive)
        {
            Assert.Contains(ids, id => id == $"money.net-per-round.5-1.{challenger.Name}");
            Assert.Contains(ids, id => id == $"money.win-rate.5-40.{challenger.Name}");
            Assert.Contains(ids, id => id == $"money.clean-win-rate.5-1.{challenger.Name}");
            Assert.Contains(ids, id => id == $"money.jokerless-rate.5-1.{challenger.Name}");
            Assert.Contains(
                ids, id => id.StartsWith($"money.take-rate.5-1.{challenger.Name}", StringComparison.Ordinal));
        }

        var path = Path.Combine(Path.GetTempPath(), $"burmese-poker-{Guid.NewGuid():N}", "measurements.csv");

        try
        {
            SuiteCsv.WriteTo(path, report);

            var lines = File.ReadAllLines(path);

            Assert.Equal(report.Measurements.Count + 1, lines.Length);
            Assert.StartsWith("id,subject,metric,games,mean,error,interval,verdict", lines[0], StringComparison.Ordinal);

            // Every row has the same number of fields, quoted commas and all — a file the docs
            // quote has to survive being read by something other than a person.
            Assert.All(lines, line => Assert.Equal(12, Fields(line)));
        }
        finally
        {
            var directory = Path.GetDirectoryName(path);

            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>Counts the fields of a CSV line, respecting quotes.</summary>
    private static int Fields(string line)
    {
        var fields = 1;
        var quoted = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                fields++;
            }
        }

        return fields;
    }
}
