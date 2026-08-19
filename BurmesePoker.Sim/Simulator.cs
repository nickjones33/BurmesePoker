using System.Diagnostics;

namespace BurmesePoker.Sim;

/// <summary>
/// Runs the games and adds up what came back.
/// </summary>
/// <remarks>
/// <b>Parallelism is a scheduling detail and nothing more.</b> Each game derives its own seed
/// from the master seed and its index (<see cref="SeedSequence"/>), owns its own engine, agents
/// and observer, and lands in its own slot of the results array; the aggregation that follows
/// sums integers in game order. So a run gives the same answer on one core as on sixteen, which
/// is the acceptance test the whole packet stands on.
/// </remarks>
public static class Simulator
{
    /// <summary>Plays a whole run.</summary>
    public static SimulationReport Run(SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validated();

        var games = new GameResult[options.Games];
        var clock = Stopwatch.StartNew();

        if (options.Parallel)
        {
            var settings = new ParallelOptions();

            if (options.MaxDegreeOfParallelism is { } limit)
            {
                settings.MaxDegreeOfParallelism = limit;
            }

            System.Threading.Tasks.Parallel.For(0, options.Games, settings, game =>
            {
                games[game] = GameRunner.Play(options, game);
            });
        }
        else
        {
            for (var game = 0; game < options.Games; game++)
            {
                games[game] = GameRunner.Play(options, game);
            }
        }

        clock.Stop();

        return Summarise(options, games, clock.Elapsed);
    }

    /// <summary>
    /// Adds a set of played games up. <b>Shared with <see cref="Replay"/></b> so a replayed run
    /// is summarised by the same arithmetic as the run it came from (BUILD-PLAN P14).
    /// </summary>
    internal static SimulationReport Summarise(
        SimulationOptions options,
        IReadOnlyList<GameResult> games,
        TimeSpan elapsed)
    {
        var tallies = new Dictionary<string, StrategyTally>();

        foreach (var strategy in options.Strategies)
        {
            tallies[strategy.Name] = new StrategyTally(strategy.Name);
        }

        var rounds = 0;
        var turns = 0L;
        var reshuffles = 0;
        var abandoned = 0;

        foreach (var game in games)
        {
            if (game.Abandoned)
            {
                abandoned++;
            }

            foreach (var round in game.Rounds)
            {
                rounds++;
                turns += round.Turns;
                reshuffles += round.Reshuffles;

                foreach (var seat in round.Seats)
                {
                    tallies[seat.Strategy].Add(seat, round.Turns);
                }
            }
        }

        return new SimulationReport(
            options,
            games,
            rounds,
            turns,
            reshuffles,
            abandoned,
            [.. options.Strategies.Select(strategy => tallies[strategy.Name].Summarise())],
            elapsed);
    }
}
