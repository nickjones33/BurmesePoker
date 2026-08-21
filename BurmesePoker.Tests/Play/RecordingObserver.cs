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
    private int? _drawsBeforeFirstReshuffle;

    public List<string> Events { get; } = [];

    public RoundResult? Settled { get; private set; }

    /// <summary>Every count of <c>TableState.AllCards</c> seen, one per event.</summary>
    public List<int> CardsInPlay { get; } = [];

    /// <summary>Every distinct-card count seen. Differs from <see cref="CardsInPlay"/> only if a card was cloned.</summary>
    public List<int> DistinctCardsInPlay { get; } = [];

    /// <summary>Hand sizes seen after each completed discard, flattened across seats.</summary>
    public List<int> HandSizesBetweenTurns { get; } = [];

    /// <summary>Every blind draw, in order — who drew and what.</summary>
    public List<(PlayerId Player, Card Card)> Draws { get; } = [];

    /// <summary>How many cards each reshuffle gathered (RULES.md §5). Empty in most rounds.</summary>
    public List<int> Reshuffles { get; } = [];

    /// <summary>
    /// Every ownership as it stood at the first reshuffle. Nothing recorded before that moment
    /// may change afterwards — ownership is write-once (RULES.md §5).
    /// </summary>
    public Dictionary<CardId, PlayerId> OwnersAtFirstReshuffle { get; } = [];

    /// <summary>The draws that happened after the first reshuffle.</summary>
    public List<(PlayerId Player, Card Card)> DrawsAfterFirstReshuffle =>
        [.. Draws.Skip(_drawsBeforeFirstReshuffle ?? Draws.Count)];

    public void Watch(TableState table)
    {
        _table = table;
        Check();
    }

    public void RoundStarted(int round, IReadOnlyList<PlayerId> seating, IReadOnlyList<Card> turnedUp)
    {
        Seatings.Add([.. seating]);
        Events.Add($"round {round} started, turned up {string.Join(" ", turnedUp)}");
        Check();
    }

    /// <summary>
    /// The seating each round was dealt with, in order — re-drawn every deal (RULES.md §3
    /// step 2).
    /// </summary>
    public List<IReadOnlyList<PlayerId>> Seatings { get; } = [];

    public void ClaimRefused(PlayerId objector, PlayerId claimant, Card card)
    {
        Refusals.Add((objector, claimant, card));
        Events.Add($"{objector} refused {claimant} the turned-up {card}");
        Check();
    }

    /// <summary>Every claim refused under RULES.md §4.5.</summary>
    public List<(PlayerId Objector, PlayerId Claimant, Card Card)> Refusals { get; } = [];

    public void PlayerDrew(PlayerId player, Card card)
    {
        Draws.Add((player, card));
        Events.Add($"{player} drew {card}");
        Check();
    }

    public void DiscardsReshuffled(int cards)
    {
        if (_table is not null && Reshuffles.Count == 0)
        {
            _drawsBeforeFirstReshuffle = Draws.Count;

            foreach (var (id, owner) in _table.Ownership.Records)
            {
                OwnersAtFirstReshuffle[id] = owner;
            }
        }

        Reshuffles.Add(cards);
        Events.Add($"discards reshuffled: {cards} cards");
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
