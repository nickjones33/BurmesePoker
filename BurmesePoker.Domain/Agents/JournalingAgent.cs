using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// Wraps a seat and writes down every answer it gives (BUILD-PLAN P14).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the seam BUILD-PLAN §3.8 item 2 reserved, promoted.</b> <c>RecordingAgent</c> in
/// the test project and <see cref="Play.GameJournalBuilder"/>'s other consumer <c>SeatRecorder</c>
/// in the harness are both this shape already; a journal is the same idea kept long enough to
/// be worth a file. Nothing in the engine changes to make it work — it sees an
/// <see cref="IPlayerAgent"/> and asks it five questions.
/// </para>
/// <para>
/// <b>It records answers, not intentions.</b> What the wrapped strategy would have done had it
/// been asked something else is not knowable from here, and a journal that guessed would be a
/// worse record than one that did not.
/// </para>
/// <para>
/// ⚠️ <b>P24.2 narrows that sentence rather than repealing it, and the narrowing is deliberate.</b>
/// A seat may be given a <see cref="ISecondOpinion"/>, and then the discard is written down with
/// <em>somebody else's</em> answer beside it (<see cref="JournalAdvice"/>). That is not a guess at
/// the player's intention — it is a fact about the game, taken on the same
/// <see cref="TurnContext"/> — and it is what turns <em>where the expert disagreed with the
/// computer</em> from something a person has to notice into a query
/// (<see cref="JournalDecision.DisagreedWithTheComputer"/>).
/// </para>
/// <para>
/// ⚠️ <b>Only a seat a person is playing gets one.</b> A bot seat's advice is its own answer, so
/// recording it would run the adviser twice a turn to learn nothing.
/// </para>
/// </remarks>
/// <param name="inner">The strategy actually deciding — a bot, or a person at a keyboard.</param>
/// <param name="journal">Where the answers go. One per game, shared by that game's seats.</param>
/// <param name="advice">
/// Who to ask for a second opinion on the discard, or null — which is every bot seat and every
/// table nobody asked for advice at.
/// </param>
public sealed class JournalingAgent(IPlayerAgent inner, GameJournalBuilder journal, ISecondOpinion? advice = null)
    : IPlayerAgent
{
    private readonly IPlayerAgent _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly GameJournalBuilder _journal = journal ?? throw new ArgumentNullException(nameof(journal));
    private readonly ISecondOpinion? _advice = advice;

    /// <summary>Wraps every seat of a table in one journal.</summary>
    /// <param name="agents">The seats, as the engine will hold them.</param>
    /// <param name="journal">Where the answers go.</param>
    /// <param name="opinions">
    /// Which seats get a second opinion recorded beside their discard, and whose. Absent seats
    /// are journalled exactly as they always were (P24.2).
    /// </param>
    public static Dictionary<PlayerId, IPlayerAgent> Wrap(
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        GameJournalBuilder journal,
        IReadOnlyDictionary<PlayerId, ISecondOpinion>? opinions = null)
    {
        ArgumentNullException.ThrowIfNull(agents);

        return agents.ToDictionary(
            seat => seat.Key,
            IPlayerAgent (seat) => new JournalingAgent(
                seat.Value,
                journal,
                opinions is not null && opinions.TryGetValue(seat.Key, out var opinion) ? opinion : null));
    }

    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = _journal.SnapshotOf(context);
        var action = _inner.ChooseAction(context);
        _journal.Append(JournalDecision.Of(context.Round, context.TurnNumber, context.Player, action, snapshot));
        return action;
    }

    /// <remarks>
    /// ⚠️ <b>The opinion is taken before the seat answers</b>, exactly as the snapshot is: what is
    /// being recorded is what the computer would have done <em>facing this</em>, and the engine
    /// discards from the seat's own list the moment the answer comes back (P13.1).
    /// </remarks>
    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = _journal.SnapshotOf(context);
        var advice = _advice?.OnDiscard(context);
        var discard = _inner.ChooseDiscard(context);

        _journal.Append(
            JournalDecision.Of(context.Round, context.TurnNumber, context.Player, discard, snapshot)
                with { Advice = advice });

        return discard;
    }

    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = _journal.SnapshotOf(context);
        var claimed = _inner.ClaimTurnedUpMoneyCard(context);

        _journal.Append(JournalDecision.Of(
            context.Round, context.TurnNumber, context.Player, JournalQuestion.Claim, claimed, snapshot));

        return claimed;
    }

    public bool ObjectToClaim(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = _journal.SnapshotOf(context);
        var objected = _inner.ObjectToClaim(context);

        _journal.Append(JournalDecision.Of(
            context.Round, context.TurnNumber, context.Player, JournalQuestion.Objection, objected, snapshot));

        return objected;
    }

    public bool Declare(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = _journal.SnapshotOf(context);
        var declared = _inner.Declare(context);

        _journal.Append(JournalDecision.Of(
            context.Round, context.TurnNumber, context.Player, JournalQuestion.Declare, declared, snapshot));

        return declared;
    }

    /// <remarks>
    /// ⚠️ <b>Turn 0, and no snapshot.</b> The seating is settled in the gap before a round is
    /// dealt (RULES.md §3 step 2), so there is no turn to record and no fourteen to photograph —
    /// and a replay looks the answer up by <c>(round, 0)</c>.
    /// </remarks>
    public SeatingOpinion AskAboutTheSeating(SeatingQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        var opinion = _inner.AskAboutTheSeating(question);
        _journal.Append(JournalDecision.Of(question.Round, 0, question.Player, opinion));
        return opinion;
    }
}
