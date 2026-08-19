using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Server;

/// <summary>
/// One thing that happened at the table, as a connection is allowed to hear it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <see cref="Domain.Abstractions.IGameObserver"/> with the concealment applied.</b>
/// The engine narrates everything, including a blind draw's card, and deliberately so — deciding
/// who may see what is the front end's job (P8). Over a connection that decision stops being a
/// courtesy and becomes a security property (BUILD-PLAN §3.10), so it is made once, here, by
/// <see cref="TableFanOut"/>, and every consumer downstream is given only what it may have.
/// </para>
/// <para>
/// ⚠️ <b><see cref="Drew"/> is the only event that differs by listener</b>, and it is the whole
/// point: the player who drew is told the card and everybody else is told only that a card was
/// drawn. Every other event is already public by the rules — a discard, a taken discard, a
/// claimed money card and a declaration all happen in front of the table (RULES.md §5, §6.3).
/// </para>
/// <para>
/// ⚠️ <b>Every list in here is a snapshot.</b> The lists the engine narrates with are its own
/// live ones — <c>TableState.TurnedUpOnTable</c> loses a card when the opening player claims it
/// — and a connection holds what it was given until it next draws (P13.1). The fan-out copies
/// on the way in; nothing here aliases the table.
/// </para>
/// <para>
/// A closed hierarchy: the constructor is private, so the cases below are all there are and a
/// consumer can switch over them exhaustively.
/// </para>
/// </remarks>
public abstract record TableEvent
{
    private TableEvent()
    {
    }

    /// <summary>The deal is done and the money cards are turned up (RULES.md §3).</summary>
    public sealed record RoundStarted(int Round, IReadOnlyList<Card> TurnedUp) : TableEvent;

    /// <summary>
    /// Somebody sat down in a seat that was waiting for a player, and this is what they are
    /// called from now on (BUILD-PLAN P13.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Public, and it has to be.</b> Who is at a table is the most public fact there is —
    /// everybody in the room can see the chair fill. It carries a name and a seat and nothing
    /// else, so there is no hand anywhere near it.
    /// </para>
    /// <para>
    /// ⚠️ <b>A seat is named when somebody sits in it, not when the table is opened.</b> A lobby
    /// opens a table before it knows who is coming, so the name arrives later — which is why
    /// this event exists at all rather than the name being settled by <c>TableSeat</c>.
    /// </para>
    /// </remarks>
    public sealed record SeatTaken(PlayerId Player, string Name) : TableEvent;

    /// <summary>
    /// Whoever was in a seat has gone, and the computer will be playing it (BUILD-PLAN P13.6).
    /// </summary>
    /// <remarks>
    /// <b>The seat keeps their name</b>, because <em>"Nick's seat is being played by the
    /// computer"</em> is the true thing to say and <em>"Seat 2"</em> is not. §3.11 C16: a
    /// dropped circuit must not look like a dropped game.
    /// </remarks>
    public sealed record SeatLeft(PlayerId Player, string Name) : TableEvent;

    /// <summary>
    /// The table has turned to a seat and is waiting on it. <b>Whose turn it is, and nothing
    /// else about the turn</b> (BUILD-PLAN P13.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>Broadcast, deliberately, after asking whether it may be.</b> Until P13.5 the
    /// nearest the public game got to <em>whose turn</em> was <em>whose move was last</em>,
    /// which a client with seats at positions cannot spotlight without lying — the glyph would
    /// point at the seat that had just finished. So the question was put properly: <b>whose
    /// turn it is leaks nothing</b>. Everybody at a real table can see who is being waited on;
    /// the concealment is about <em>what is in a hand</em> (RULES.md §7.1), and this event
    /// carries no card, no count and no hand. <c>ConcealmentTests</c> covers it rather than
    /// merely still passing.
    /// </para>
    /// <para>
    /// ⚠️ <b>Once per turn, not once per question.</b> A turn asks between two and four
    /// questions depending on what the seat does, and a seat that was asked more of them is not
    /// having a longer turn — it is the same <c>(Round, TurnNumber)</c> key
    /// <see cref="SeatPlayedByTheComputer"/> and P11's pacing decorator both use.
    /// </para>
    /// <para>
    /// ⚠️ <b>It is raised by <c>BoundedAgent</c>, which wraps every seat</b> — a bot's turn is
    /// as public as a person's, and a spotlight that only lit the seats somebody had connected
    /// to would be worse than none.
    /// </para>
    /// </remarks>
    public sealed record TurnBegan(PlayerId Player, int Round, int Turn) : TableEvent;

    /// <summary>
    /// A player drew blind. <paramref name="Card"/> is the card for the player who drew it and
    /// <b>null for everybody else</b> — the one filtered event, and the reason this type exists.
    /// </summary>
    public sealed record Drew(PlayerId Player, Card? Card) : TableEvent;

    /// <summary>A player took the previous player's discard. Public, and confers no ownership.</summary>
    public sealed record TookDiscard(PlayerId Player, Card Card) : TableEvent;

    /// <summary>The opening player took the top money card off the table (RULES.md §4.5).</summary>
    public sealed record MoneyCardClaimed(PlayerId Player, Card Card) : TableEvent;

    /// <summary>A player discarded. Public, and ownership is unaffected (RULES.md §4.4).</summary>
    public sealed record Discarded(PlayerId Player, Card Card) : TableEvent;

    /// <summary>The draw pile ran out and the discards became the new one (RULES.md §5).</summary>
    public sealed record DiscardsReshuffled(int Cards) : TableEvent;

    /// <summary>A player laid down all 13 cards and ended the round (RULES.md §7.1).</summary>
    public sealed record Declared(PlayerId Player, IReadOnlyList<Meld> Melds) : TableEvent;

    /// <summary>The money has been worked out.</summary>
    public sealed record Settled(RoundResult Result) : TableEvent;

    /// <summary>
    /// Nobody answered for that seat in time, so the computer played it (BUILD-PLAN P13.2).
    /// </summary>
    /// <remarks>
    /// <b>Reported rather than silent</b>, which is the difference between a seat being played
    /// and a table appearing to freeze — §3.11 C16, where a client says <em>"Sable is playing
    /// your seat"</em>. Broadcast to the whole table, once per turn however many of that turn's
    /// questions went unanswered.
    /// </remarks>
    public sealed record SeatPlayedByTheComputer(PlayerId Player, int Round, int Turn) : TableEvent;

    /// <summary>
    /// The round ran past the table's wall-clock limit and was given up on
    /// (BUILD-PLAN P13.2).
    /// </summary>
    /// <remarks>
    /// <b>The host's decision, not the engine's.</b> Only a declaration ends a round
    /// (RULES.md §7.1), so a table whose people all walk away plays for ever; P12 bounds a
    /// round by turn count and reports it, and a host wants the same bounded by the clock.
    /// </remarks>
    public sealed record TableAbandoned(int Round, TimeSpan Limit) : TableEvent;
}
