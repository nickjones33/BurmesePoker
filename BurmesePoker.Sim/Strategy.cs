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
/// Makes a new agent for one seat of one game, from that seat's seed. <b>Called per seat,
/// never shared</b> — an agent that remembered anything across games would make a run depend
/// on the order its games happened to be scheduled in, which is exactly the determinism P12
/// stands on.
/// <para>
/// ⚠️ <b>The seed is the argument because a strategy may not reach for
/// <see cref="Random.Shared"/></b> (BUILD-PLAN §3.7 item 1, P15). Rungs that decide nothing at
/// random ignore it; <see cref="RandomBotAgent"/> is why it is here at all.
/// </para>
/// </param>
public sealed record Strategy(string Name, Func<int, IPlayerAgent> Create);

/// <summary>The strategies the command line can name.</summary>
public static class StrategyCatalog
{
    /// <summary>
    /// Everything nameable, <b>in ladder order</b>: each rung differs from the one above it in
    /// exactly one decision (BUILD-PLAN P15).
    /// </summary>
    /// <remarks>
    /// <c>random</c> chooses legally and thinks about nothing; <c>simple</c> throws whatever
    /// costs it the fewest melded cards; <c>greedy</c> is <c>simple</c> with a tie-break
    /// towards the cards worth keeping; <c>cautious</c> is <c>greedy</c> with the remaining
    /// ties decided by what the discard is worth to whoever picks it up.
    /// </remarks>
    public static IReadOnlyList<Strategy> All { get; } =
    [
        new("random", seed => new RandomBotAgent(new Random(seed))),
        new("simple", _ => new SimpleBotAgent()),
        new("greedy", _ => new GreedyBotAgent()),
        new("cautious", _ => new CautiousBotAgent())
    ];

    /// <summary>Looks a strategy up by name, case-insensitively.</summary>
    public static Strategy Resolve(string name) =>
        All.FirstOrDefault(strategy => string.Equals(strategy.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException(
            $"No strategy called '{name}'. Known: {string.Join(", ", All.Select(s => s.Name))}.", nameof(name));
}
