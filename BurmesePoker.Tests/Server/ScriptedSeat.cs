using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Server;

namespace BurmesePoker.Tests.Server;

/// <summary>
/// A person at a keyboard, scripted: it answers every prompt the moment the prompt arrives,
/// and writes down what it was shown.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>This is what makes P13.2 testable without a socket.</b> Blazor Server supplies the
/// transport (BUILD-PLAN §3.10), so a browser circuit holds the very
/// <see cref="SeatConnection"/> this holds — there is no wire to fake and no protocol to stub.
/// </para>
/// <para>
/// <b>It answers inside <see cref="SeatConnection.Updated"/>, which makes the whole round
/// deterministic.</b> The event is raised on the round's own thread before the seat begins
/// waiting, so the answer is latched and the wait returns at once; nothing here sleeps, races
/// or polls. A real client answers later, from a circuit, which
/// <c>TableSessionTests.AnAnswerFromAnotherThreadIsWaitedFor</c> covers separately.
/// </para>
/// <para>
/// <b>What it plays:</b> it draws from the deck, declines the turned-up money card, throws whatever the
/// computer suggests, and declares as soon as it may. Following the hint means a scripted seat
/// plays a hand the way <c>GreedyBotAgent</c> would, so a round of them finishes for the same
/// reason a bot round does — and it exercises the hint end to end, which is the one thing in
/// the prompt that costs a domain call.
/// </para>
/// </remarks>
public sealed class ScriptedSeat
{
    private readonly SeatConnection _connection;

    public ScriptedSeat(SeatConnection connection)
    {
        _connection = connection;
        connection.Updated += OnUpdated;
    }

    /// <summary>Every prompt this seat was shown, in order.</summary>
    public List<SeatPrompt> Prompts { get; } = [];

    /// <summary>How many of them it answered in time.</summary>
    public int Answered { get; private set; }

    /// <summary>Whether it should stop answering at all — a player who has walked away.</summary>
    public bool Silent { get; set; }

    /// <summary>
    /// Which prompts it is away for, if it comes and goes. Asked before every answer, so a
    /// player can miss one turn and be back for the next.
    /// </summary>
    public Func<SeatPrompt, bool>? Away { get; set; }

    private void OnUpdated(SeatConnection connection)
    {
        if (connection.Pending is not { } prompt)
        {
            return;
        }

        Prompts.Add(prompt);

        if (Silent || Away?.Invoke(prompt) == true)
        {
            return;
        }

        if (connection.Answer(Reply(prompt)))
        {
            Answered++;
        }
    }

    /// <summary>What this script answers to a given question. Shared with the threaded driver.</summary>
    internal static SeatAnswer Reply(SeatPrompt prompt) => prompt.Question switch
    {
        SeatQuestion.Take => new SeatAnswer.Take(TurnAction.DrawFromDeck),
        SeatQuestion.ClaimMoneyCard => new SeatAnswer.Claim(false),
        SeatQuestion.Discard => new SeatAnswer.Discard(Throw(prompt)),
        _ => new SeatAnswer.Declaration(true)
    };

    /// <summary>
    /// The card the computer marked, or the first loose one if the table is not offering hints.
    /// </summary>
    private static Card Throw(SeatPrompt prompt)
    {
        foreach (var card in prompt.Hand.Cards)
        {
            if (card.IsSuggestedThrow)
            {
                return card.Card;
            }
        }

        return prompt.Hand.Loose.Count > 0 ? prompt.Hand.Loose[0].Card : prompt.Hand.Cards[0].Card;
    }
}
