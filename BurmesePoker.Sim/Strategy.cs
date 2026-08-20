using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;

namespace BurmesePoker.Sim;

/// <summary>
/// A way of playing, and how to seat a fresh one.
/// </summary>
/// <param name="Name">
/// What results are attributed to. It is half of every row's join key (BUILD-PLAN §3.8 item 4),
/// so it must be stable: rename a strategy and yesterday's numbers stop being comparable.
/// <para>
/// ⚠️ <b>A strategy name is not quite a bot name</b> (P18). Every rung of
/// <see cref="BotCatalog"/> is a strategy, but the tournament's null cell seats one rung twice
/// under <c>"{name}#mirror"</c> (<see cref="Tournament.MirrorSuffix"/>) — a label on a copy,
/// used to measure the harness's own bias. That is a thing this record may carry and the
/// catalog may not, which is why the two types stayed separate.
/// </para>
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
public sealed record Strategy(string Name, Func<int, IPlayerAgent> Create)
{
    /// <summary>The same rung, as the harness names it.</summary>
    internal static Strategy Of(BotRung rung) => new(rung.Name, rung.Create);

    /// <summary>
    /// The same difficulty level, as the harness names it.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The harness ranks levels as well as rungs, and that is not the same as offering
    /// both in a menu</b> (BUILD-PLAN §3.12, P19). A level's whole claim — that <em>n+1</em>
    /// beats <em>n</em> and a person can tell — is a measurement, so it has to be measurable by
    /// the instrument that measures everything else. What §3.12 forbids is a <em>front end</em>
    /// showing a person a list of research instruments beside a list of settings.
    /// </remarks>
    internal static Strategy Of(DifficultyLevel level) => new(level.Name, level.Create);
}

/// <summary>
/// The strategies the command line can name.
/// </summary>
/// <remarks>
/// ⚠️ <b>An adapter over <see cref="BotCatalog"/> since P18, and nothing more.</b> The ladder
/// itself lives in the domain beside the agents it names, so that a new rung reaches the
/// console, the browser and this harness at once instead of three times. What is left here is
/// the harness's own vocabulary — a <see cref="Strategy"/> can wear a mirror label and a
/// <c>BotRung</c> cannot — and the order, which is the ladder's and is what every report and
/// every CSV is ordered by.
/// </remarks>
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
    public static IReadOnlyList<Strategy> All { get; } = [.. BotCatalog.All.Select(Strategy.Of)];

    /// <summary>
    /// The difficulty dial, <b>weakest first</b> — the product rather than the instrument
    /// (BUILD-PLAN §3.12).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Not part of <see cref="All"/>, which is what a report defaults to.</b> A run that
    /// silently mixed rungs and levels would be ranking a research instrument against a product
    /// built out of it, and every level is the hardest rung with a handicap — so <c>hard</c>
    /// and <c>greedy</c> are the same player under two names, which is a null cell, not a
    /// comparison.
    /// </remarks>
    public static IReadOnlyList<Strategy> Levels { get; } = [.. DifficultyLadder.All.Select(Strategy.Of)];

    /// <summary>
    /// Looks a strategy up by name, case-insensitively: a rung, a difficulty level, or a
    /// calibration probe such as <c>greedy@0.35</c>.
    /// </summary>
    public static Strategy Resolve(string name) =>
        All.FirstOrDefault(strategy => string.Equals(strategy.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? (DifficultyLadder.FindOrProbe(name) is { } level
            ? Strategy.Of(level)
            : throw new ArgumentException(
                $"No strategy called '{name}'. Known: {string.Join(", ", All.Select(s => s.Name))}; "
                + $"the difficulty levels {string.Join(", ", Levels.Select(s => s.Name))}; and a probe such "
                + $"as {BotCatalog.Hardest.Name}{DifficultyLevel.Reserved}0.35.",
                nameof(name)));
}
