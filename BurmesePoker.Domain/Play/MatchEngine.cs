using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// A session at one table: round after round, with the banks carrying over (RULES.md §7.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing ends a match</b> (RULES.md §7.2). There is no target score and no round limit;
/// players stop when they choose, so this engine plays a round whenever it is asked to and
/// never decides that the session is over. Asking <em>"another round?"</em> belongs to
/// whoever is running the table, not here — it is not a move, so it is not an
/// <see cref="IPlayerAgent"/> question either.
/// </para>
/// <para>
/// <b>A round is a fresh <see cref="RoundEngine"/>, and that is what re-designates the money
/// cards.</b> The registry is a pure function of the two cards turned up, built in
/// <see cref="TableState"/>'s constructor and never mutated (BUILD-PLAN §3.3), so nothing
/// carries over between rounds except the banks — which is exactly where the 2023 design's
/// non-idempotent re-marking bug lived.
/// </para>
/// <para>
/// <b>Each round hands back its table as well as its result</b> (BUILD-PLAN §3.8). Settlement
/// returns net deltas only, and splitting them into the flat round payment and the money-card
/// side bet needs <see cref="TableState.Ownership"/> and <see cref="TableState.Shoe"/>; so
/// does asking how close the losers were. Both live on the table, so <see cref="PlayRound()"/>
/// returns the pair. Nothing is retained afterwards — a consumer that wants a history keeps
/// one, and a million-round simulation does not pay for tables it has already read.
/// </para>
/// </remarks>
public sealed class MatchEngine
{
    private readonly Dictionary<PlayerId, IPlayerAgent> _agents;
    private readonly Dictionary<PlayerId, int> _banks;
    private readonly IGameObserver? _observer;
    private readonly Random _random;

    /// <param name="players">The seating order, which is fixed for the match (RULES.md §3 step 2).</param>
    /// <param name="agents">One agent per player.</param>
    /// <param name="stakes">What every round of this match is played for.</param>
    /// <param name="random">
    /// The one source of randomness: each round's shuffle, and any reshuffle within a round.
    /// A match is reproducible from its seed alone (BUILD-PLAN §3.7).
    /// </param>
    /// <param name="observer">Narration sink, passed to every round. Optional.</param>
    public MatchEngine(
        IReadOnlyList<PlayerId> players,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        Stakes stakes,
        Random random,
        IGameObserver? observer = null)
    {
        ArgumentNullException.ThrowIfNull(stakes);
        ArgumentNullException.ThrowIfNull(random);

        _agents = RoundEngine.RequireAPlayableTable(players, agents);
        _random = random;
        _observer = observer;

        Players = [.. players];
        Stakes = stakes;
        _banks = Players.ToDictionary(player => player, _ => 0);
    }

    /// <summary>The seating order, which is also the turn order every round (RULES.md §3).</summary>
    public IReadOnlyList<PlayerId> Players { get; }

    /// <summary>What every round of this match is played for.</summary>
    public Stakes Stakes { get; }

    /// <summary>
    /// Each player's running total, everyone starting at zero and positive meaning ahead.
    /// A live view.
    /// </summary>
    /// <remarks>
    /// Conservation is a property of the addition: a round's payouts always sum to zero
    /// (P6), so the banks always sum to zero too, however many rounds are played.
    /// </remarks>
    public IReadOnlyDictionary<PlayerId, int> Banks => _banks;

    /// <summary>How many rounds have been played and banked.</summary>
    public int RoundsPlayed { get; private set; }

    /// <summary>Plays the next round from a freshly shuffled shoe.</summary>
    public RoundRecord PlayRound()
    {
        var deck = Deck.TwoDecks();
        deck.Shuffle(_random);
        return PlayRound([.. deck.Cards]);
    }

    /// <summary>
    /// Plays the next round from a given draw order — the scriptable entry point, mirroring
    /// <see cref="RoundEngine"/>'s own constructor.
    /// </summary>
    /// <param name="drawOrder">
    /// The whole 108-card shoe in the order it will leave the deck, top first.
    /// </param>
    public RoundRecord PlayRound(IReadOnlyList<Card> drawOrder)
    {
        var engine = new RoundEngine(
            Players, _agents, Stakes, drawOrder, _random, RoundsPlayed + 1, _observer);

        var result = engine.Play();
        RoundsPlayed++;

        foreach (var (player, delta) in result.Payouts)
        {
            _banks[player] += delta;
        }

        return new RoundRecord(result, engine.Table);
    }
}

/// <summary>
/// One played round: how it ended, and the table it ended on.
/// </summary>
/// <param name="Result">Who won, the cover they laid down, the money that moved, and the turn count.</param>
/// <param name="Table">
/// The table as it stood at settlement. <b>The only route to two things a consumer cannot
/// otherwise have</b> (BUILD-PLAN §3.8): what the money-card side bet contributed, which needs
/// <see cref="TableState.Ownership"/> and <see cref="TableState.Shoe"/>, and how close the
/// losers were, which needs <see cref="TableState.Seats"/>.
/// </param>
public sealed record RoundRecord(RoundResult Result, TableState Table);
