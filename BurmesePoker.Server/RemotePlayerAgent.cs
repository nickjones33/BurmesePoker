using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;

namespace BurmesePoker.Server;

/// <summary>
/// A seat played by somebody who is not in this process: the agent puts the question to a
/// connection, waits for the answer, and lets the computer play the move if none arrives.
/// </summary>
/// <remarks>
/// <para>
/// <b>§3.6 cashed.</b> The decision that agents stay synchronous was taken before P10, on the
/// bet that a remote player could simply block inside one. This is that bet being collected:
/// <see cref="IPlayerAgent"/> has not moved, so a browser seat costs the domain nothing, and
/// the engine cannot tell this seat from a bot — which is also what makes the takeover below
/// possible at all.
/// </para>
/// <para>
/// <b>A timeout is a bot move</b> (BUILD-PLAN P13). The stand-in answers <em>the same
/// <see cref="TurnContext"/></em> the absent player was given, and needs no handover because a
/// strategy keeps no state between turns (P15). It is per question rather than per seat: a
/// player who misses one prompt and comes back for the next one is simply back.
/// </para>
/// <para>
/// ⚠️ <b>The takeover is announced, once a turn.</b> A turn asks a varying number of questions,
/// so announcing per question would make a silent seat noisier on some turns than others for
/// no reason a watcher could see — the same argument, and the same
/// <c>(Round, TurnNumber)</c> key, that P11's pacing decorator uses.
/// </para>
/// <para>
/// ⚠️ <b>Pacing is not applied here.</b> P11 gives a bot seat a beat so it reads as a seat
/// playing rather than as nothing happening, and a taken-over seat wants the same — but a sleep
/// belongs to whatever is drawing the table, not to a server that may be hosting many of them.
/// <see cref="TableOptions.StandIn"/> is a factory for exactly that reason: a host that draws
/// hands over an agent that is already paced.
/// </para>
/// </remarks>
public sealed class RemotePlayerAgent : IPlayerAgent
{
    private readonly SeatConnection _connection;
    private readonly IPlayerAgent _standIn;
    private readonly TableFanOut _table;
    private readonly TimeSpan _patience;
    private readonly ComputerAdvice? _advice;
    private (int Round, int Turn) _announced;

    /// <param name="connection">The mailbox this seat's player is holding.</param>
    /// <param name="standIn">Who plays the seat when nobody answers in time.</param>
    /// <param name="table">Where the takeover is announced.</param>
    /// <param name="patience">How long to wait for an answer. Zero means do not wait at all.</param>
    /// <param name="advice">
    /// The computer's suggested throw, marked in the hand the player is shown, or null when the
    /// table is not offering hints. ⚠️ <b>Never re-derived</b> — see <see cref="ComputerAdvice"/>.
    /// </param>
    public RemotePlayerAgent(
        SeatConnection connection,
        IPlayerAgent standIn,
        TableFanOut table,
        TimeSpan patience,
        ComputerAdvice? advice = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(standIn);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentOutOfRangeException.ThrowIfLessThan(patience, TimeSpan.Zero);

        if (connection.Player is null)
        {
            throw new ArgumentException("A watcher holds no seat and is never asked anything.", nameof(connection));
        }

        _connection = connection;
        _standIn = standIn;
        _table = table;
        _patience = patience;
        _advice = advice;
    }

    public TurnAction ChooseAction(TurnContext context) =>
        Ask(context, SeatQuestion.Take) is SeatAnswer.Take answer
            ? answer.Action
            : _standIn.ChooseAction(context);

    public bool ClaimTurnedUpMoneyCard(TurnContext context) =>
        Ask(context, SeatQuestion.ClaimMoneyCard) is SeatAnswer.Claim answer
            ? answer.Claimed
            : _standIn.ClaimTurnedUpMoneyCard(context);

    public bool ObjectToClaim(TurnContext context) =>
        Ask(context, SeatQuestion.ObjectToClaim) is SeatAnswer.Objection answer
            ? answer.Objected
            : _standIn.ObjectToClaim(context);

    public Card ChooseDiscard(TurnContext context) =>
        Ask(context, SeatQuestion.Discard) is SeatAnswer.Discard answer
            ? answer.Card
            : _standIn.ChooseDiscard(context);

    public bool Declare(TurnContext context) =>
        Ask(context, SeatQuestion.Declare) is SeatAnswer.Declaration answer
            ? answer.Declared
            : _standIn.Declare(context);

    private SeatAnswer? Ask(TurnContext context, SeatQuestion question)
    {
        ArgumentNullException.ThrowIfNull(context);

        // 🔥 The arrow is only ever about which card to throw (P11, P13.1) — but the *why* is
        // asked of every question, because P24.2's scope is all of them and the arrow reading as
        // a promise of a sentence that is not there is the bug report the packet came from.
        // ⚠️ One call: the rationale carries the card it is about, so the discard's ranking is
        // bought once and the hint comes off it (P24.2 acceptance 2).
        var rationale = _advice is null ? null : Why(context, question);
        var suggested = question == SeatQuestion.Discard ? rationale?.Advised : null;
        var answer = _connection.Ask(SeatPrompt.For(context, question, suggested, rationale), _patience);

        if (answer is null)
        {
            Announce(context);
        }

        return answer;
    }

    /// <summary>
    /// The computer's opinion about the question standing, whichever of the five it is.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Every arm is named and there is no catch-all</b>, for <c>JournalFormat.Name</c>'s
    /// reason: a default arm is a mistranslation waiting for the next case, and this one would
    /// silently explain a new question as though it were an old one.
    /// </remarks>
    private AdviceRationale? Why(TurnContext context, SeatQuestion question) => question switch
    {
        SeatQuestion.Take => _advice!.WhyTake(context),
        SeatQuestion.Discard => _advice!.WhyThrow(context),
        SeatQuestion.ClaimMoneyCard => _advice!.WhyClaim(context),
        SeatQuestion.ObjectToClaim => _advice!.WhyObject(context),
        SeatQuestion.Declare => _advice!.WhyDeclare(context),
        _ => null
    };

    private void Announce(TurnContext context)
    {
        if ((context.Round, context.TurnNumber) == _announced)
        {
            return;
        }

        _announced = (context.Round, context.TurnNumber);
        _table.Broadcast(
            new TableEvent.SeatPlayedByTheComputer(context.Player, context.Round, context.TurnNumber));
    }
}
