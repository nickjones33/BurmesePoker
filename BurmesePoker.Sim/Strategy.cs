using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;

namespace BurmesePoker.Sim;

/// <summary>
/// A way of playing, and how to seat a fresh one.
/// </summary>
/// <param name="Name">
/// What results are attributed to. It is half of every row's join key (BUILD-PLAN §3.8 item 4),
/// so it must be stable: rename a strategy and yesterday's numbers stop being comparable.
/// </param>
/// <param name="Create">
/// Makes a new agent for one seat of one game. <b>Called per seat, never shared</b> — an agent
/// that remembered anything across games would make a run depend on the order its games
/// happened to be scheduled in, which is exactly the determinism P12 stands on.
/// </param>
public sealed record Strategy(string Name, Func<IPlayerAgent> Create);

/// <summary>The strategies the command line can name.</summary>
public static class StrategyCatalog
{
    /// <summary>Everything nameable, in the order it is offered.</summary>
    public static IReadOnlyList<Strategy> All { get; } =
    [
        new("greedy", () => new GreedyBotAgent()),
        new("simple", () => new SimpleBotAgent())
    ];

    /// <summary>Looks a strategy up by name, case-insensitively.</summary>
    public static Strategy Resolve(string name) =>
        All.FirstOrDefault(strategy => string.Equals(strategy.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException(
            $"No strategy called '{name}'. Known: {string.Join(", ", All.Select(s => s.Name))}.", nameof(name));
}
