using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Domain.Agents;

/// <summary>
/// A seat that answers from a journal (BUILD-PLAN P14).
/// </summary>
/// <remarks>
/// <para>
/// <b>Replay is a strategy, not a mode.</b> This is an ordinary <see cref="IPlayerAgent"/>, so
/// playing a game back is playing it with different seats — no second engine, no resumable
/// state machine, and a human seat replays exactly as a computer one does.
/// </para>
/// <para>
/// <b>Divergence is loud, always.</b> The journal's answers are consumed in order and each is
/// checked against the question actually being asked; a mismatch, a card the seat is not
/// holding, or running out of file all throw <see cref="JournalException"/> naming the
/// decision. A replay that carried on regardless would look successful while being a different
/// game, which is the one failure mode a journal exists to rule out.
/// </para>
/// <para>
/// Answers left over at the end are not an error: a game abandoned mid-round leaves that
/// round's decisions in the file, and a replay of its settled rounds simply stops before them.
/// </para>
/// </remarks>
public sealed class JournalPlayerAgent : IPlayerAgent
{
    private readonly PlayerId _player;
    private readonly List<JournalDecision> _decisions;
    private int _next;

    /// <param name="journal">The game to answer from.</param>
    /// <param name="player">Which seat of it this agent is playing.</param>
    public JournalPlayerAgent(GameJournal journal, PlayerId player)
    {
        ArgumentNullException.ThrowIfNull(journal);

        _player = player;
        _decisions = [.. journal.Decisions.Where(decision => decision.Player == player)];
    }

    /// <summary>One of these per seat of a journal, ready to hand to a <see cref="MatchEngine"/>.</summary>
    public static Dictionary<PlayerId, IPlayerAgent> SeatsOf(GameJournal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        return journal.Header.Players.ToDictionary(
            player => player,
            IPlayerAgent (player) => new JournalPlayerAgent(journal, player));
    }

    public TurnAction ChooseAction(TurnContext context) => Next(context, JournalQuestion.Action).AsAction();

    public Card ChooseDiscard(TurnContext context)
    {
        var decision = Next(context, JournalQuestion.Discard);
        var id = decision.AsCardId();

        foreach (var card in context.Hand)
        {
            if (card.Id == id)
            {
                return card;
            }
        }

        throw new JournalException(
            $"Round {decision.Round} turn {decision.Turn}, {_player}: the journal throws card {id}, "
            + $"which is not in the hand ({string.Join(", ", context.Hand.Select(card => card.Id))}). "
            + "The replay has diverged from the game that was recorded.");
    }

    public bool ClaimTurnedUpMoneyCard(TurnContext context) => Next(context, JournalQuestion.Claim).AsBoolean();

    public bool ObjectToClaim(TurnContext context) => Next(context, JournalQuestion.Objection).AsBoolean();

    public bool Declare(TurnContext context) => Next(context, JournalQuestion.Declare).AsBoolean();

    /// <remarks>
    /// <para>
    /// 🔥 <b>This is what makes a re-seating replayable.</b> The seating a round is dealt to is a
    /// consequence of what every seat said in the gap before it, so a replay that consented on
    /// everybody's behalf would deal the same cards to different people from the next round on.
    /// ⚠️ <b>Looked up at turn 0</b>, which is where <c>JournalingAgent</c> writes it.
    /// </para>
    /// <para>
    /// 🔥 <b>The one question that peeks rather than consuming, and absence means
    /// <see cref="SeatingOpinion.Consent"/>.</b> Every journal written before P37 has no seating
    /// decisions at all, and the rule those games were played under is the rule consent gives
    /// back: the seats did not move. ⚠️ <b>So this is deliberately quieter than the other five</b>,
    /// which is a narrowing of <em>divergence is loud, always</em> and not a repeal of it —
    /// consent changes nothing on its own, so the only journal this can answer silently is one
    /// where nothing happened to record.
    /// </para>
    /// </remarks>
    public SeatingOpinion AskAboutTheSeating(SeatingQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (_next >= _decisions.Count)
        {
            return SeatingOpinion.Consent;
        }

        var next = _decisions[_next];

        if (next.Question != JournalQuestion.Seating)
        {
            return SeatingOpinion.Consent;
        }

        return Next(question.Round, turn: 0, JournalQuestion.Seating).AsSeatingOpinion();
    }

    private JournalDecision Next(TurnContext context, JournalQuestion question)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Next(context.Round, context.TurnNumber, question);
    }

    private JournalDecision Next(int round, int turn, JournalQuestion question)
    {
        if (_next >= _decisions.Count)
        {
            throw new JournalException(
                $"The journal runs out at round {round} turn {turn}: {_player} was asked "
                + $"{question} and there is nothing left to answer with. The journal is short or truncated.");
        }

        var decision = _decisions[_next++];

        return decision.Round == round
            && decision.Turn == turn
            && decision.Question == question
                ? decision
                : throw new JournalException(
                    $"The journal disagrees with the game at round {round} turn {turn}: "
                    + $"{_player} was asked {question}, but the next thing written down is {decision.Question} "
                    + $"at round {decision.Round} turn {decision.Turn}. The replay has diverged.");
    }
}
