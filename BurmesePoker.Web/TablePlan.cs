using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Web;

/// <summary>
/// What to open a table with, before anybody is at it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The lobby is not a domain concept</b> (BUILD-PLAN P13.6). It decides seating and names
/// and then constructs exactly what <c>Program</c> constructed before it existed — so this
/// record is the whole of what a lobby knows, and none of it reaches the engine.
/// </para>
/// <para>
/// ⚠️ <b><see cref="People"/> is the count, not the seats.</b> Which seat you end up in is
/// decided when you sit down, and the person seats are the first ones dealt so that a solo
/// table puts you where the console does. A table with <c>People = 0</c> is P13.3's room with
/// nobody in it, which is still worth watching and is the strictest case for concealment.
/// </para>
/// </remarks>
public sealed record TablePlan
{
    /// <summary>What this table is called in the lobby.</summary>
    public required string Title { get; init; }

    /// <summary>How many seats it has (RULES.md §2.1).</summary>
    public required int Seats { get; init; }

    /// <summary>How many of them are waiting for a person; the rest are the computer's.</summary>
    public required int People { get; init; }

    /// <summary>The one seed this whole table is reproducible from (§3.9).</summary>
    public required int Seed { get; init; }

    /// <summary>How long a computer seat waits before moving, so a person can follow it (P11).</summary>
    public TimeSpan Pace { get; init; } = TimeSpan.FromMilliseconds(1100);

    /// <summary>How long the table sits still between a settlement and the next deal.</summary>
    public TimeSpan BetweenRounds { get; init; } = TimeSpan.FromSeconds(12);

    /// <summary>How long a seat is given to answer before the computer plays the move.</summary>
    public TimeSpan Patience { get; init; } = TimeSpan.FromSeconds(90);

    /// <summary>Whether the computer's suggestions are worked out for the seats at all.</summary>
    public bool Hints { get; init; } = true;

    /// <summary>
    /// How long one round may run before the table gives up on it, or null for no limit.
    /// </summary>
    /// <remarks>
    /// <b>The host's own setting, and it was unreachable from a plan until P65.</b> Only a
    /// declaration ends a round (RULES.md §7.1), so a table nobody answers at never finishes on
    /// its own and the limit is what stops it; the default is the session's own.
    /// ⚠️ <b>It is not the patience</b> — that is one question's wait, this is the whole round's.
    /// </remarks>
    public TimeSpan? RoundTimeLimit { get; init; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Where to write this table down as it plays, or null to keep no journal (P24.1).
    /// </summary>
    /// <remarks>
    /// <b>The console's <c>--journal</c>, for a hosted table</b> — the same file format, written
    /// after every settled round rather than at exit, because nothing ends a hosted match but
    /// the table closing. The lobby's form does not offer it: two tables writing one path would
    /// take turns overwriting each other, so it stays a command-line request for the table the
    /// site was started with.
    /// </remarks>
    public string? Journal { get; init; }

    /// <summary>
    /// How hard the computer is at this table — the name of a level of
    /// <c>DifficultyLadder</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>A name and not a level</b> (BUILD-PLAN P18). A plan is what a table is opened with:
    /// it comes off a command line, out of a form field and — one day — out of a saved table,
    /// all of which carry text. The ladder is asked to resolve it once, where the table is
    /// built, and an unknown name falls back to the default rather than throwing a site over.
    /// </para>
    /// <para>
    /// ⚠️ <b>The single-value shorthand for <see cref="Difficulties"/></b> (P19): it sets every
    /// computer seat, which is what most tables want and all tables wanted before P19.
    /// </para>
    /// <para>
    /// ⚠️ <b>It is the opponents' setting and nothing else's.</b> The stand-in that plays a seat
    /// nobody is answering, and the hint your own seat is offered, are both
    /// <c>BotCatalog.Hardest</c> — <em>the rung</em>, not a level — whatever this says. A hint
    /// that got worse as you lowered the difficulty would be absurd, and a seat taken over
    /// should not start playing badly. Said in three places because it looks like an oversight
    /// in all three.
    /// </para>
    /// </remarks>
    public string Difficulty { get; init; } = DifficultyLadder.Default.Name;

    /// <summary>
    /// A level for each computer seat, in seating order, or null for
    /// <see cref="Difficulty"/> at all of them.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Difficulty is per seat, because a table of four identical bots is the least
    /// interesting table in the game</b> (BUILD-PLAN P19). The list is cycled if it is shorter
    /// than the computer seats and ignored past the end if it is longer, so a plan cannot be
    /// wrong about a table's shape — the shape is decided by <see cref="Seats"/> and
    /// <see cref="People"/>, and this only says who is in the seats that are left.
    /// </remarks>
    public IReadOnlyList<string>? Difficulties { get; init; }

    /// <summary>
    /// How long this table's seating holds — the name of a <c>SeatingPolicy</c> (P36).
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>A name and not a policy</b>, for the reason <see cref="Difficulty"/> is a name and
    /// not a level (P18): a plan is what a table is opened with, and it comes off a command line
    /// and out of a form field, both of which carry text. It is resolved once, where the table is
    /// built, and an unknown name opens on the default rather than throwing a site over.
    /// </para>
    /// <para>
    /// ✅ <b>The default is <c>held</c>, which is the rule</b> (RULES.md §3 step 2, rev 28) — and
    /// it is what stops the ring rearranging itself around a fixed viewer every deal (P13.5).
    /// </para>
    /// </remarks>
    public string Seating { get; init; } = SeatingPolicy.Default.Name;
}
