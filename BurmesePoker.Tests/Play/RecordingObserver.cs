using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// Writes down everything the engine narrates, and re-checks the table's invariants at every
/// event it hears about.
/// </summary>
/// <remarks>
/// <see cref="Watch"/> is called after the engine is constructed, so the invariant checks
/// cover the whole round bar the deal itself — which the tests assert directly.
/// </remarks>
internal sealed class RecordingObserver : IGameObserver
{
    private TableState? _table;

    public List<string> Events { get; } = [];

    public RoundResult? Settled { get; private set; }

    /// <summary>Every count of <c>TableState.AllCards</c> seen, one per event.</summary>
    public List<int> CardsInPlay { get; } = [];

    /// <summary>Every distinct-card count seen. Differs from <see cref="CardsInPlay"/> only if a card was cloned.</summary>
    public List<int> DistinctCardsInPlay { get; } = [];

    /// <summary>Hand sizes seen after each completed discard, flattened across seats.</summary>
    public List<int> HandSizesBetweenTurns { get; } = [];

    public void Watch(TableState table)
    {
        _table = table;
        Check();
    }

    public void RoundStarted(int round, IReadOnlyList<Card> turnedUp)
    {
        Events.Add($"round {round} started, turned up {string.Join(" ", turnedUp)}");
        Check();
    }

    public void PlayerDrew(PlayerId player, Card card)
    {
        Events.Add($"{player} drew {card}");
        Check();
    }

    public void PlayerTookDiscard(PlayerId player, Card card)
    {
        Events.Add($"{player} took {card}");
        Check();
    }

    public void MoneyCardClaimed(PlayerId player, Card card)
    {
        Events.Add($"{player} claimed {card}");
        Check();
    }

    public void PlayerDiscarded(PlayerId player, Card card)
    {
        Events.Add($"{player} discarded {card}");
        Check();

        if (_table is not null)
        {
            HandSizesBetweenTurns.AddRange(_table.Seats.Select(seat => seat.Hand.Count));
        }
    }

    public void PlayerDeclared(PlayerId player, IReadOnlyList<Meld> melds)
    {
        Events.Add($"{player} declared {melds.Count} melds");
        Check();
    }

    public void RoundSettled(RoundResult result)
    {
        Settled = result;
        Events.Add($"{result.Winner} won round {result.Round}");
        Check();
    }

    private void Check()
    {
        if (_table is null)
        {
            return;
        }

        var cards = _table.AllCards.ToList();
        CardsInPlay.Add(cards.Count);
        DistinctCardsInPlay.Add(cards.Select(card => card.Id).Distinct().Count());
    }
}
