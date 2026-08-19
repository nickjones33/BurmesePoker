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
/// <see cref="IPlayerAgent"/> and asks it four questions.
/// </para>
/// <para>
/// <b>It records answers, not intentions.</b> What the wrapped strategy would have done had it
/// been asked something else is not knowable from here, and a journal that guessed would be a
/// worse record than one that did not.
/// </para>
/// </remarks>
/// <param name="inner">The strategy actually deciding — a bot, or a person at a keyboard.</param>
/// <param name="journal">Where the answers go. One per game, shared by that game's seats.</param>
public sealed class JournalingAgent(IPlayerAgent inner, GameJournalBuilder journal) : IPlayerAgent
{
    private readonly IPlayerAgent _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly GameJournalBuilder _journal = journal ?? throw new ArgumentNullException(nameof(journal));

    /// <summary>Wraps every seat of a table in one journal.</summary>
    public static Dictionary<PlayerId, IPlayerAgent> Wrap(
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        GameJournalBuilder journal)
    {
        ArgumentNullException.ThrowIfNull(agents);

        return agents.ToDictionary(
            seat => seat.Key,
            IPlayerAgent (seat) => new JournalingAgent(seat.Value, journal));
    }

    public TurnAction ChooseAction(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = _journal.SnapshotOf(context);
        var action = _inner.ChooseAction(context);
        _journal.Append(JournalDecision.Of(context.Round, context.TurnNumber, context.Player, action, snapshot));
        return action;
    }

    public Card ChooseDiscard(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = _journal.SnapshotOf(context);
        var discard = _inner.ChooseDiscard(context);
        _journal.Append(JournalDecision.Of(context.Round, context.TurnNumber, context.Player, discard, snapshot));
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

    public bool Declare(TurnContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var snapshot = _journal.SnapshotOf(context);
        var declared = _inner.Declare(context);

        _journal.Append(JournalDecision.Of(
            context.Round, context.TurnNumber, context.Player, JournalQuestion.Declare, declared, snapshot));

        return declared;
    }
}
