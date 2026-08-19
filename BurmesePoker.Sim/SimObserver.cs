using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>
/// Counts what the engine narrates, for one round at a time.
/// </summary>
/// <remarks>
/// Every event here passes what the engine already holds, and this observer only adds to
/// integers — BUILD-PLAN §3.8 item 3's constraint, which matters at a million rounds: an
/// observer that copied a hand or formatted a string in a hot event would cost more than the
/// game does.
/// </remarks>
public sealed class SimObserver : IGameObserver
{
    private readonly Dictionary<PlayerId, int> _draws;
    private readonly Dictionary<PlayerId, int> _takes;

    /// <param name="players">The seating, so every seat has a tally whether or not it acts.</param>
    public SimObserver(IReadOnlyList<PlayerId> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        _draws = players.ToDictionary(player => player, _ => 0);
        _takes = players.ToDictionary(player => player, _ => 0);
    }

    /// <summary>How often the draw pile was rebuilt from the discards, this round (RULES.md §5).</summary>
    public int Reshuffles { get; private set; }

    /// <summary>Starts a fresh round's tallies.</summary>
    public void BeginRound()
    {
        foreach (var player in _draws.Keys)
        {
            _draws[player] = 0;
            _takes[player] = 0;
        }

        Reshuffles = 0;
    }

    /// <summary>Cards this seat took blind off the deck, this round — the only route to ownership.</summary>
    public int DrawsBy(PlayerId player) => _draws[player];

    /// <summary>Cards this seat took from the player before it, this round.</summary>
    public int TakesBy(PlayerId player) => _takes[player];

    public void PlayerDrew(PlayerId player, Card card) => _draws[player]++;

    public void PlayerTookDiscard(PlayerId player, Card card) => _takes[player]++;

    public void DiscardsReshuffled(int cards) => Reshuffles++;
}
