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
/// <param name="Strategy">
/// What a journal attributes this seat's answers to — a difficulty level's name, a rung's, or
/// null for the kind's own word (<c>human</c> for a remote seat, <c>bot</c> for the computer's).
/// Half of a CSV row's join key (BUILD-PLAN §3.8 item 4), exactly as the console writes it.
/// </param>
public sealed record TableSeat(PlayerId Player, string Name, IPlayerAgent? Agent = null, string? Strategy = null)
{
    /// <summary>A seat somebody will connect to and play.</summary>
    public static TableSeat Person(PlayerId player, string name) => new(player, name);

    /// <summary>A seat the computer plays.</summary>
    public static TableSeat Computer(PlayerId player, string name, IPlayerAgent agent, string? strategy = null)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return new TableSeat(player, name, agent, strategy);
    }

    /// <summary>Whether this seat is waiting for somebody to connect.</summary>
    public bool IsRemote => Agent is null;

    /// <summary>The journal's word for this seat (P24.1).</summary>
    public string Attribution => Strategy ?? (IsRemote ? "human" : "bot");
}
