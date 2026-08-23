using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;
using BurmesePoker.Server;

namespace BurmesePoker.Web;

/// <summary>
/// Your seat, as you have it: the question you are being asked, and the hand it is about.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The private counterpart of <see cref="TableBoard"/>, and it exists for the same
/// reason.</b> The board is the public game folded out of a watcher's events; this is the one
/// seat's game folded out of <em>its own</em> connection — the prompts it was sent and the
/// round boundaries it was told about, <b>and nothing else</b>. A component that wanted a hand
/// from anywhere else would have to go round the fan-out, which is the thing
/// <c>ConcealmentTests</c> stands in front of (BUILD-PLAN P13.4 acceptance 3).
/// </para>
/// <para>
/// ⚠️ <b>Everything here is a snapshot</b> (P13.1). A <see cref="SeatPrompt"/> already copied
/// the hand it was built from, which is what makes it safe to hold across a render — and a
/// browser holds every view across a render.
/// </para>
/// <para>
/// <b>Why there is a hand at all when nobody is asking.</b> After you discard, your thirteen
/// cards are fixed until your next turn: nobody draws from your hand, and the card another
/// player takes comes off your discard pile. So the hand you are shown while the bots play is
/// not stale — it is <see cref="HandView"/> rebuilt over the twelve or thirteen you kept, with
/// the money and the ownership taken from the view you were sent rather than worked out again.
/// </para>
/// <para>
/// <b>Threading.</b> <see cref="SeatConnection.Updated"/> is raised on the round's own thread
/// before the seat begins waiting (P13.2), and an answer arrives from a circuit; the state
/// behind the two is guarded, and <see cref="Changed"/> is always raised outside the lock so a
/// renderer can never be waiting on a round.
/// </para>
/// </remarks>
public sealed class SeatBoard : IDisposable
{
    private readonly Lock _gate = new();
    private readonly SeatConnection _connection;
    private int _seen;
    private bool _left;

    public SeatBoard(SeatConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.Player is null)
        {
            throw new ArgumentException(
                "A watcher holds no seat and is never asked anything.", nameof(connection));
        }

        _connection = connection;
        _connection.Updated += OnTold;

        lock (_gate)
        {
            Read();
        }
    }

    /// <summary>Which seat is yours.</summary>
    public PlayerId Player => _connection.Player!.Value;

    /// <summary>What the table calls you.</summary>
    public string Name => _connection.Name;

    /// <summary>The question standing in front of you, or null when it is not your turn.</summary>
    public SeatPrompt? Asking { get; private set; }

    /// <summary>
    /// Your hand: the one the question is about while you are being asked, and the one you kept
    /// while the others play. Null between the settlement and the next deal.
    /// </summary>
    public HandView? Hand { get; private set; }

    /// <summary>How many questions you have answered in time.</summary>
    public int Answered { get; private set; }

    /// <summary>
    /// Whether the table is waiting on you.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Being asked something is no longer the same as being on turn</b> (P28). The claim's
    /// permission is put to the seat that plays <em>before</em> the opener (RULES.md §4.5), so a
    /// question can stand in front of you in the middle of somebody else's turn — which is why
    /// this reads the question rather than the spotlight, and why the spotlight is
    /// <c>TableBoard.Turn</c> and not this.
    /// </remarks>
    public bool IsYourTurn => Asking is not null;

    /// <summary>Raised whenever any of the above changed. May arrive on any thread.</summary>
    public event Action? Changed;

    /// <summary>Take the discard, or draw blind (RULES.md §5).</summary>
    public bool Take(TurnAction action) => Answer(new SeatAnswer.Take(action));

    /// <summary>Claim the top turned-up card instead of drawing (RULES.md §4.5).</summary>
    public bool Claim(bool claimed) => Answer(new SeatAnswer.Claim(claimed));

    /// <summary>
    /// Refuse the seat after you the turned-up money card (RULES.md §4.5). <b>Asked while it is
    /// not your turn</b>, and only when you are holding that rank.
    /// </summary>
    public bool Object(bool objected) => Answer(new SeatAnswer.Objection(objected));

    /// <summary>Throw a card away, which ends your turn (RULES.md §5).</summary>
    public bool Throw(Card card) => Answer(new SeatAnswer.Discard(card));

    /// <summary>Lay all thirteen down and end the round (RULES.md §7.1).</summary>
    public bool Declare(bool declared) => Answer(new SeatAnswer.Declaration(declared));

    /// <summary>
    /// What this seat has said about changing the seating (RULES.md §3 step 2, §9 #45).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It is not an answer to <see cref="Asking"/></b> and it never will be: the seating
    /// question is public and is put to every seat at once, so it lives beside the turn rather
    /// than inside it. A seat that has said nothing consents, and consent moves nothing.
    /// </remarks>
    public SeatingOpinion Seating => _left ? SeatingOpinion.Consent : _connection.Seating;

    /// <summary>
    /// Say something about changing the seats — asked of the table, not of the turn.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>No route to the table session, exactly as with every other control on the page</b>
    /// (P13.4 acceptance 3): the connection broadcasts it, because saying it is a public act
    /// (<c>TableEvent.SeatingOpinionGiven</c>). ⚠️ <b>A seat that has stood up says nothing</b>,
    /// the same rule <see cref="Answer"/> follows.
    /// </remarks>
    public bool Says(SeatingOpinion opinion)
    {
        lock (_gate)
        {
            if (_left)
            {
                return false;
            }
        }

        return _connection.SaysAboutTheSeating(opinion);
    }

    /// <summary>
    /// Answer the question standing. <b>False means it was refused, and the question stands</b> —
    /// never that the round is over.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A client bug must not end somebody's round</b> (P13.2). <c>SeatConnection.Answer</c>
    /// refuses an answer that does not fit the question or names a card the seat is not holding,
    /// rather than throwing the way <c>RoundEngine</c> does — so all this has to do is say what
    /// it is really being asked and let the page draw it again.
    /// </remarks>
    public bool Answer(SeatAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        SeatPrompt? asked;

        lock (_gate)
        {
            asked = _left ? null : Asking;
        }

        if (asked is null)
        {
            return false;
        }

        if (!_connection.Answer(answer))
        {
            // ⚠️ Silent when the question still stands, which is the ordinary case and the one
            // that must not raise anything: a handler that answers on Changed would answer the
            // same refused question again, and again. Something is said only if the question
            // really has moved on — a seat that took too long and was played for.
            bool moved;

            lock (_gate)
            {
                var stood = Asking;
                Read();
                moved = !ReferenceEquals(stood, Asking);
            }

            if (moved)
            {
                Changed?.Invoke();
            }

            return false;
        }

        lock (_gate)
        {
            Answered++;

            // Answered, so the question is done with — said now rather than when the round
            // thread gets round to clearing it, because a control that stays live after it was
            // pressed is a control somebody presses twice.
            Asking = null;

            if (answer is SeatAnswer.Discard thrown && Hand is { } held)
            {
                Hand = Without(held, thrown.Card, asked.MoneyCards);
            }
        }

        Changed?.Invoke();
        return true;
    }

    /// <summary>
    /// Stands up out of the seat: this board stops listening and stops answering.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Stops <em>answering</em>, which is the half that is not tidiness</b> (P13.6). A
    /// browser refresh is a new circuit and Blazor holds the old one for minutes before
    /// disposing it, so a player who reloaded takes their own seat back off their own ghost —
    /// and a ghost still holding a live <see cref="SeatConnection"/> could still press a card.
    /// A seat somebody has stood up from answers nothing, whoever is holding it.
    /// </remarks>
    public void Dispose()
    {
        _connection.Updated -= OnTold;

        lock (_gate)
        {
            _left = true;
            Asking = null;
        }
    }

    private void OnTold(SeatConnection connection)
    {
        lock (_gate)
        {
            if (_left)
            {
                return;
            }

            Read();
        }

        Changed?.Invoke();
    }

    /// <summary>Everything this seat knows, taken from its connection. Call under the lock.</summary>
    private void Read()
    {
        var events = _connection.Events;

        // A hand belongs to a round. The prompts never say a round ended — only the narration
        // does — so the deal and the settlement are read from the events, and from nothing more
        // than that: what a seat may see is decided by the fan-out and not by this.
        for (var index = _seen; index < events.Count; index++)
        {
            if (events[index] is TableEvent.RoundStarted or TableEvent.Settled or TableEvent.TableAbandoned)
            {
                Hand = null;
            }
        }

        _seen = events.Count;

        Asking = _connection.Pending;

        if (Asking is { } prompt)
        {
            Hand = prompt.Hand;
        }
    }

    /// <summary>
    /// The hand you kept, after throwing one card away.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Nothing is decided here that the server decided already.</b> The money multiplier
    /// comes from the prompt's own registry and ownership is read back off the cards you were
    /// sent — the near-melds and the per-card cost are all this recomputes, because they are
    /// about thirteen cards now rather than fourteen. <b>The computer's suggestion is not
    /// carried over</b>: it was advice about a throw you have made.
    /// </remarks>
    private static HandView Without(HandView view, Card thrown, MoneyCardRegistry money)
    {
        // Instance identity (§3.1): the other copy of the same card stays in your hand.
        var kept = view.Hand.Where(card => card.Id != thrown.Id).ToArray();

        return kept.Length == view.Hand.Count
            ? view
            : HandView.Of(
                kept,
                money,
                card => view.Of(card).IsOwned,
                // A card stays face up for as long as it stays in the hand (RULES.md §5.2), so
                // the marks carry over from the view you were sent — less the card just thrown.
                faceUp: [.. view.Cards.Where(card => card.IsFaceUp && card.Card.Id != thrown.Id).Select(card => card.Card)]);
    }
}
