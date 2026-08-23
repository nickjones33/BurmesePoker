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
    /// Whether it refuses a claim it is asked to permit. True by default, which is what every
    /// rung does — <b>an allowed objection is the answer no shipped player ever gives</b>, so a
    /// test that wants one must say so (review R6).
    /// </summary>
    public bool Objects { get; set; } = true;

    /// <summary>
    /// Which prompts it is away for, if it comes and goes. Asked before every answer, so a
    /// player can miss one turn and be back for the next.
    /// </summary>
    public Func<SeatPrompt, bool>? Away { get; set; }

    /// <summary>
    /// Whether it takes the previous seat's discard the first time one is offered, instead of
    /// drawing. Once, so the round still converges the way a hint-following table does.
    /// </summary>
    /// <remarks>
    /// For P41: an open take is the one thing that puts a card face up (RULES.md §5.2), and the
    /// default script never makes one — every draw is blind, which is exactly what the
    /// concealment tests want from the fixture round and exactly what a face-up test cannot
    /// use.
    /// </remarks>
    public bool TakesTheDiscardOnce { get; set; }

    private bool _tookTheDiscard;

    /// <summary>
    /// Whether it deliberately throws something <em>other</em> than the card the computer marked.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>For P24.2 acceptance 3, and it has to be a real disagreement.</b> The journal records
    /// the computer's opinion beside the seat's answer so that
    /// <c>JournalDecision.DisagreedWithTheComputer</c> picks the arguments out of a file; a test
    /// in which every seat follows the hint would assert that against nothing.
    /// </remarks>
    public bool Contrarian { get; set; }

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

        SeatAnswer reply;

        if (TakesTheDiscardOnce && !_tookTheDiscard
            && prompt.Question == SeatQuestion.Take && prompt.AvailableDiscard is not null)
        {
            _tookTheDiscard = true;
            reply = new SeatAnswer.Take(TurnAction.TakeDiscard);
        }
        else
        {
            reply = prompt.Question == SeatQuestion.ObjectToClaim
                ? new SeatAnswer.Objection(Objects)
                : Contrarian && prompt.Question == SeatQuestion.Discard
                    ? new SeatAnswer.Discard(SomethingElse(prompt))
                    : Reply(prompt);
        }

        if (connection.Answer(reply))
        {
            Answered++;
        }
    }

    /// <summary>What this script answers to a given question. Shared with the threaded driver.</summary>
    internal static SeatAnswer Reply(SeatPrompt prompt) => prompt.Question switch
    {
        SeatQuestion.Take => new SeatAnswer.Take(TurnAction.DrawFromDeck),
        SeatQuestion.ClaimMoneyCard => new SeatAnswer.Claim(false),
        SeatQuestion.ObjectToClaim => new SeatAnswer.Objection(true),
        SeatQuestion.Discard => new SeatAnswer.Discard(Throw(prompt)),
        _ => new SeatAnswer.Declaration(true)
    };

    /// <summary>
    /// The card the computer marked, or — at a table not offering hints — the card just taken.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Throwing back what you took is the fallback because it <em>terminates</em>.</b> The
    /// obvious one, <em>the first loose card</em>, does not: the hand it leaves is the hand it
    /// started from rearranged, and a table of seats doing that runs until the clock stops it
    /// (found by turning the hints off in <c>TableJournalTests</c>). Throwing the taken card back
    /// leaves the hand exactly as it was, so this seat simply stands still while the bots at the
    /// table race to thirteen — which is what a test about the <em>record</em> wants from a seat.
    /// ⚠️ It is filtered like any other card (RULES.md §5.1, §9 #13), so where the ban has closed
    /// its rank the first legal card is taken instead.
    /// </remarks>
    private static Card Throw(SeatPrompt prompt)
    {
        foreach (var card in prompt.Hand.Cards)
        {
            if (card.IsSuggestedThrow)
            {
                return card.Card;
            }
        }

        if (prompt.Taken is { } taken && prompt.MayThrow(taken))
        {
            return taken;
        }

        foreach (var card in prompt.Hand.Cards)
        {
            if (prompt.MayThrow(card.Card))
            {
                return card.Card;
            }
        }

        return prompt.Hand.Cards[0].Card;
    }

    /// <summary>
    /// Any card but the computer's — and a <b>legal</b> one, because the feeding ban makes a
    /// banned card an impossible move rather than a wrong answer (RULES.md §5.1).
    /// </summary>
    private static Card SomethingElse(SeatPrompt prompt)
    {
        var suggested = Throw(prompt);

        foreach (var card in prompt.Hand.Cards)
        {
            if (card.Card.Id != suggested.Id && prompt.MayThrow(card.Card))
            {
                return card.Card;
            }
        }

        return suggested;
    }
}
