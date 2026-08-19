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
}
