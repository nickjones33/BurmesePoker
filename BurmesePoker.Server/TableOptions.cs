using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Money;

namespace BurmesePoker.Server;

/// <summary>
/// How one table is run: what it plays for, how long it waits, and what happens when nobody
/// answers.
/// </summary>
/// <remarks>
/// <b>A table carries its seed</b>, exactly as a console match does (P11, P14). It is the
/// bug-report format for this game and there is one <see cref="Random"/> per table, built from
/// it and drawn from by nothing else — a second draw before the first round would make the same
/// seed deal a different game, which is the mistake P14 had to unpick in the console.
/// </remarks>
public sealed record TableOptions
{
    /// <summary>The one seed the whole table is reproducible from.</summary>
    public required int Seed { get; init; }

    /// <summary>What every round is played for.</summary>
    public Stakes Stakes { get; init; } = Stakes.Standard;

    /// <summary>
    /// How long a seat is given to answer before the computer plays the move. Zero does not
    /// wait at all, which is how a test makes a takeover deterministic.
    /// </summary>
    public TimeSpan Patience { get; init; } = TimeSpan.FromSeconds(45);

    /// <summary>
    /// How long a round may run before the host gives up on it, or null for no limit.
    /// </summary>
    /// <remarks>
    /// Only a declaration ends a round (RULES.md §7.1) and the cards circulate for ever after
    /// the reshuffle, so a table with nobody at it never finishes on its own. Generous by
    /// default: a real round is minutes, and the limit is there to stop an abandoned table
    /// rather than to hurry a slow one.
    /// </remarks>
    public TimeSpan? RoundTimeLimit { get; init; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Whether the hand a player is shown carries the computer's suggested throw
    /// (<c>--no-hints</c>'s equivalent, P11).
    /// </summary>
    public bool Hints { get; init; } = true;

    /// <summary>
    /// What plays a seat whose player has stopped answering.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A factory, and per seat, for two reasons.</b> A host that draws the table wants to
    /// wrap this in P11's pacing decorator so a taken-over seat reads as a seat playing rather
    /// than as a freeze — and the sleep that does it belongs to whatever is drawing, never to a
    /// server hosting many tables. The hardest bot is the default, on P13.1's principle that a
    /// stand-in playing worse than the table does would be worth less than no stand-in.
    /// <para>
    /// ⚠️ <b>The default is <see cref="BotCatalog.Hardest"/> — <em>the rung</em>, and not the table's difficulty level</b>
    /// (BUILD-PLAN P18). It is the same argument as the hint's: a seat the computer took over
    /// because nobody was answering should not also start playing badly, and a host that wants
    /// the table's own setting here says so by supplying this factory.
    /// </para>
    /// </remarks>
    public Func<IPlayerAgent> StandIn { get; init; } = () => BotCatalog.Hardest.Create(0);
}
