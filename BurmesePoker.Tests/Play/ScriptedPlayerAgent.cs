using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// One player's turns, written down in advance. Drives a whole round deterministically with
/// no console anywhere near it.
/// </summary>
/// <remarks>
/// Once the script runs out the agent falls back to the passive turn — draw blind, throw back
/// what it drew, never declare — so filler seats need no script at all. The script advances by
/// <see cref="TurnContext.TurnNumber"/> rather than by call count, so it does not care which
/// decisions a given turn happens to ask for.
/// </remarks>
internal sealed class ScriptedPlayerAgent : IPlayerAgent
{
    private static readonly ScriptedTurn Passively = new();

    private readonly Queue<ScriptedTurn> _turns;
    private ScriptedTurn _current = Passively;
    private int _turn;

    public ScriptedPlayerAgent(params ScriptedTurn[] turns) => _turns = new Queue<ScriptedTurn>(turns);

    /// <summary>A seat that just plays out the round without trying to win.</summary>
    public static ScriptedPlayerAgent Passive() => new();

    /// <summary>Hand sizes seen before taking a card, to pin the 13-between-turns rule.</summary>
    public List<int> HandSizeBeforeTaking { get; } = [];

    /// <summary>Hand sizes seen when discarding, to pin the 14-during-a-turn rule.</summary>
    public List<int> HandSizeWhenDiscarding { get; } = [];

    /// <summary>Every claim this seat was asked to permit (RULES.md §4.5).</summary>
    public List<ClaimRequest> Objections { get; } = [];

    /// <summary>Whether it refuses the seat after it the turned-up money card (RULES.md §4.5).</summary>
    public bool Objects { get; set; }

    public bool ClaimTurnedUpMoneyCard(TurnContext context)
    {
        Sync(context);
        HandSizeBeforeTaking.Add(context.Hand.Count);
        return _current.Claim;
    }

    /// <summary>
    /// Whether this seat refuses the claim it is being asked about (RULES.md §4.5).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>A standing answer rather than a scripted turn, because it is not a turn.</b> This is
    /// asked of a seat that is not on turn, during somebody else's, so it neither reads nor
    /// advances the script — a seat asked for permission has not had a turn.
    /// </remarks>
    public bool ObjectToClaim(TurnContext context)
    {
        Objections.Add(context.PermissionAsked!);
        return Objects;
    }

    public TurnAction ChooseAction(TurnContext context)
    {
        Sync(context);
        HandSizeBeforeTaking.Add(context.Hand.Count);
        return _current.Action;
    }

    public Card ChooseDiscard(TurnContext context)
    {
        Sync(context);
        HandSizeWhenDiscarding.Add(context.Hand.Count);

        if (_current.Discard is null)
        {
            return context.Taken ?? throw new InvalidOperationException("Nothing was taken this turn.");
        }

        var wanted = Hands.Value(_current.Discard);
        var held = context.Hand.Select((card, index) => (card, index))
            .Where(entry => entry.card.SameValueAs(wanted))
            .Select(entry => entry.card)
            .ToList();

        return held.Count > 0
            ? held[0]
            : throw new InvalidOperationException($"{context.Player} holds no {_current.Discard} to discard.");
    }

    public bool Declare(TurnContext context)
    {
        Sync(context);
        return _current.Declare;
    }

    private void Sync(TurnContext context)
    {
        if (context.TurnNumber == _turn)
        {
            return;
        }

        _turn = context.TurnNumber;
        _current = _turns.Count > 0 ? _turns.Dequeue() : Passively;
    }
}

/// <summary>One scripted turn: how to take a card, what to throw, and whether to go out.</summary>
internal sealed record ScriptedTurn
{
    /// <summary>How to take a card. Not asked on a turn with no discard available.</summary>
    public TurnAction Action { get; init; } = TurnAction.DrawFromDeck;

    /// <summary>Which card to throw, by value. Null throws back whatever was taken.</summary>
    public string? Discard { get; init; }

    /// <summary>Take the turned-up money card instead of drawing. Opening turn only.</summary>
    public bool Claim { get; init; }

    /// <summary>Go out, if the engine offers.</summary>
    public bool Declare { get; init; }
}
