using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Server;

/// <summary>
/// The one place the concealment rule is applied to narration: the engine tells this
/// everything, and it tells each connection only what that connection may hear.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is the packet's security boundary</b> (BUILD-PLAN §3.10, §3.11 A1). P8 settled
/// that filtering private information is the front end's job, which was right for a hotseat
/// console where the only reader is the person whose turn it is. Over a connection it becomes
/// a property somebody could exploit rather than a courtesy somebody could forget — <b>a
/// viewer is not <em>sent</em> what it may not see</b>, and hiding a card in the markup is a
/// leak, not a fix.
/// </para>
/// <para>
/// <b>Almost everything is already public.</b> Play is fully concealed but the events are not:
/// a discard, a taken discard, a claimed money card and a declaration all happen in front of
/// the table (RULES.md §5, §6.3, §4.5, §7.1). <b>Exactly one event is private — the blind
/// draw</b> — so this class is one <c>if</c> and a great deal of care about lists.
/// </para>
/// <para>
/// ⚠️ <b>Lists are copied on the way in.</b> The engine narrates with its own live collections;
/// <c>TableState.TurnedUpOnTable</c> loses a card when the opening player claims one, and a
/// connection that kept the reference would find its record of the deal quietly changing under
/// it. P13.1 hit exactly this with a hand; a snapshot is the rule everywhere now.
/// </para>
/// </remarks>
public sealed class TableFanOut : IGameObserver
{
    private readonly Lock _gate = new();
    private readonly List<SeatConnection> _connections = [];

    /// <summary>Every connection currently attached, seated or watching.</summary>
    public IReadOnlyList<SeatConnection> Connections
    {
        get
        {
            lock (_gate)
            {
                return [.. _connections];
            }
        }
    }

    /// <summary>
    /// Attaches a connection. It hears everything from now on and nothing from before — a
    /// watcher who joins mid-round has missed the deal, which is also true at a real table.
    /// </summary>
    public void Add(SeatConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        lock (_gate)
        {
            _connections.Add(connection);
        }
    }

    /// <summary>Detaches a connection, so a dropped circuit stops being written to.</summary>
    public bool Remove(SeatConnection connection)
    {
        lock (_gate)
        {
            return _connections.Remove(connection);
        }
    }

    /// <summary>Tells every connection the same thing. For events that are public by the rules.</summary>
    public void Broadcast(TableEvent moment)
    {
        ArgumentNullException.ThrowIfNull(moment);

        foreach (var connection in Connections)
        {
            connection.Post(moment);
        }
    }

    public void RoundStarted(int round, IReadOnlyList<Card> turnedUp) =>
        // Copied: this is the table's own list, and a claim takes a card out of it.
        Broadcast(new TableEvent.RoundStarted(round, [.. turnedUp]));

    /// <remarks>
    /// <b>The one filtered event.</b> The drawer is told the card; everyone else is told that a
    /// card was drawn and nothing more. Everything a table legitimately knows about a blind
    /// draw is that it happened.
    /// </remarks>
    public void PlayerDrew(PlayerId player, Card card)
    {
        var mine = new TableEvent.Drew(player, card);
        var theirs = new TableEvent.Drew(player, null);

        foreach (var connection in Connections)
        {
            connection.Post(connection.Player == player ? mine : theirs);
        }
    }

    public void PlayerTookDiscard(PlayerId player, Card card) =>
        Broadcast(new TableEvent.TookDiscard(player, card));

    public void MoneyCardClaimed(PlayerId player, Card card) =>
        Broadcast(new TableEvent.MoneyCardClaimed(player, card));

    public void PlayerDiscarded(PlayerId player, Card card) =>
        Broadcast(new TableEvent.Discarded(player, card));

    public void DiscardsReshuffled(int cards) =>
        Broadcast(new TableEvent.DiscardsReshuffled(cards));

    public void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds) =>
        Broadcast(new TableEvent.Declared(player, [.. melds]));

    public void RoundSettled(RoundResult result) =>
        Broadcast(new TableEvent.Settled(result));
}
