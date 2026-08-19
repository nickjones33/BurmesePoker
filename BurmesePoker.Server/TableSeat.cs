using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Server;

/// <summary>
/// One place at the table, before anybody sits in it: who it belongs to, what to call them,
/// and — for a computer seat — what plays it.
/// </summary>
/// <remarks>
/// <b>The seating is given, not decided here</b> (RULES.md §3 step 2). A round engine that
/// reshuffled its own seating could not be scripted, and the same is true one layer up: the
/// lobby seats the table (P13.5) and hands the order over, exactly as
/// <c>BurmesePoker.Console</c>'s <c>Program</c> does today.
/// </remarks>
/// <param name="Player">Which seat, in seating order.</param>
/// <param name="Name">What to call whoever is in it. Presentation only; the engine never sees it.</param>
/// <param name="Agent">
/// What plays the seat, or null for a person who will connect. <b>A bot is just another
/// <see cref="IPlayerAgent"/></b> (P10), so the table cannot tell the two apart past this line.
/// </param>
public sealed record TableSeat(PlayerId Player, string Name, IPlayerAgent? Agent = null)
{
    /// <summary>A seat somebody will connect to and play.</summary>
    public static TableSeat Person(PlayerId player, string name) => new(player, name);

    /// <summary>A seat the computer plays.</summary>
    public static TableSeat Computer(PlayerId player, string name, IPlayerAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return new TableSeat(player, name, agent);
    }

    /// <summary>Whether this seat is waiting for somebody to connect.</summary>
    public bool IsRemote => Agent is null;
}
