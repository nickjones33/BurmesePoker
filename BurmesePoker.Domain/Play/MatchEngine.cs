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
/// 🔥 <b>The seating is drawn once and held</b> (RULES.md §3 step 2, rev 28). A <em>game</em>
/// means from the turn-up to somebody going out — a game is a round — and the seats do
/// <em>not</em> change between games: <em>"in real games you don't shuffle seats every round,
/// only when people ask for it."</em> ⚠️ <b>This engine held a seating before P28, re-drew it
/// before every deal between P28 and P36, and now does neither by default</b> — it re-draws when
/// its <see cref="SeatingPolicy"/> says to, and the default policy never says to. The asking
/// itself is packet P37 (§10 #23); this is the mechanism it will answer into.
/// </para>
/// <para>
/// ⚠️ <b>The first round's seating is the one this engine was given</b>: whoever sets a table up
/// has already drawn it (a lobby, a console, a harness that assigns strategies to seats on
/// purpose), and drawing over the top of that would take the caller's arrangement away without
/// asking. So a re-draw only ever happens <em>between</em> rounds, which is the only place the
/// engine is the one that knows.
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

    /// <param name="players">
    /// Who is at the table, in the order it is seated — and, unless
    /// <paramref name="seating"/> says otherwise, the order every round is dealt in
    /// (RULES.md §3 step 2).
    /// </param>
    /// <param name="agents">One agent per player.</param>
    /// <param name="stakes">What every round of this match is played for.</param>
    /// <param name="random">
    /// The one source of randomness: each round's shuffle, and any reshuffle within a round.
    /// A match is reproducible from its seed alone (BUILD-PLAN §3.7).
    /// </param>
    /// <param name="observer">Narration sink, passed to every round. Optional.</param>
    /// <param name="seating">
    /// How long a seating holds (RULES.md §3 step 2). Null is
    /// <see cref="SeatingPolicy.Default"/>, which is <em>held</em> — the rule.
    /// </param>
    public MatchEngine(
        IReadOnlyList<PlayerId> players,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        Stakes stakes,
        Random random,
        IGameObserver? observer = null,
        SeatingPolicy? seating = null)
    {
        ArgumentNullException.ThrowIfNull(stakes);
        ArgumentNullException.ThrowIfNull(random);

        _agents = RoundEngine.RequireAPlayableTable(players, agents);
        _random = random;
        _observer = observer;

        Players = [.. players];
        Seating = Players;
        SeatingPolicy = seating ?? SeatingPolicy.Default;
        Stakes = stakes;
        _banks = Players.ToDictionary(player => player, _ => 0);
    }

    /// <summary>
    /// Who is at this table, in the order they were first seated.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Not necessarily the turn order.</b> A seating can be re-drawn — never by default, but
    /// <see cref="SeatingPolicy"/> may say so — and this is the <em>membership</em> of the table
    /// either way: <see cref="Seating"/> is where anybody drawing neighbours, a turn order or a
    /// feeding ban must read. It stays fixed so that a caller's own list of seats — banks, names,
    /// agents — does not shuffle under it.
    /// </remarks>
    public IReadOnlyList<PlayerId> Players { get; }

    /// <summary>
    /// The order the most recent round was dealt in, which is that round's turn order
    /// (RULES.md §3).
    /// </summary>
    /// <remarks>
    /// Before the first deal it is <see cref="Players"/>, because that is the seating this engine
    /// was handed — and under the default policy it stays that for ever. ✅ Every round narrates
    /// its own to <c>IGameObserver.RoundStarted</c>, so a front end never has to ask.
    /// </remarks>
    public IReadOnlyList<PlayerId> Seating { get; private set; }

    /// <summary>
    /// How long this table's seating holds (RULES.md §3 step 2).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The one place the question is answered.</b> Nothing else in the solution decides
    /// when the seats are drawn again — not a front end, not the harness — which is what keeps
    /// P37's <em>agreeing</em> a change in one file rather than in four.
    /// </remarks>
    public SeatingPolicy SeatingPolicy { get; }

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

    /// <summary>
    /// Plays the next round from a freshly shuffled shoe, and the seating this table is holding
    /// (RULES.md §3 steps 1 and 2).
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Shuffle, then seat, then deal</b> — §3's own order, and this is the only entry point
    /// that does the first two. ⚠️ <b>"Seat" is almost always a no-op</b>: a seating is drawn once
    /// and held (rev 28), so <see cref="SeatingPolicy"/> has to say otherwise before anything
    /// moves, and the first round keeps the order this engine was given whatever it says.
    /// </remarks>
    public RoundRecord PlayRound()
    {
        var deck = Deck.TwoDecks();
        deck.Shuffle(_random);
        Seating = NextSeating();

        return PlayRound([.. deck.Cards]);
    }

    /// <summary>
    /// Plays the next round from a given draw order — the scriptable entry point, mirroring
    /// <see cref="RoundEngine"/>'s own constructor.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>It deals to the table as it stands and draws no seats</b>, for the same reason
    /// <see cref="RoundEngine"/> takes its seating as given: a deal written down card by card is
    /// a deal written down <em>for a seating</em>, and a round that re-drew the seats underneath
    /// it could not be scripted at all. Re-seating is <see cref="PlayRound()"/>'s, along with the
    /// shuffle it belongs beside (RULES.md §3 steps 1 and 2).
    /// </remarks>
    /// <param name="drawOrder">
    /// The whole 108-card shoe in the order it will leave the deck, top first.
    /// </param>
    public RoundRecord PlayRound(IReadOnlyList<Card> drawOrder)
    {
        var engine = new RoundEngine(
            Seating, _agents, Stakes, drawOrder, _random, RoundsPlayed + 1, _observer);

        var result = engine.Play();
        RoundsPlayed++;

        foreach (var (player, delta) in result.Payouts)
        {
            _banks[player] += delta;
        }

        return new RoundRecord(result, engine.Table);
    }

    /// <summary>
    /// The seats for the round about to be dealt: the seating being held, unless
    /// <see cref="SeatingPolicy"/> says this is a round to draw again before (RULES.md §3 step 2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The condition is the policy's and is asked here and nowhere else.</b> A held seating
    /// is returned as it stands rather than reset to <see cref="Players"/> — the members are the
    /// membership, and the order is whatever the last draw left.
    /// </para>
    /// <para>
    /// ⚠️ <b>A draw may come out the same, and that is not a bug.</b> The rule is that the seats
    /// are randomised, not that they must move; forcing a derangement would make the seating less
    /// random rather than more.
    /// </para>
    /// <para>
    /// ⚠️ <b>A held seating draws no numbers at all</b>, which is a seed story (BUILD-PLAN §3.9
    /// point 2): a seed or a journal from between P28 and P36 no longer plays the same match,
    /// because the draw it used to take out of this generator is not taken any more. The journal
    /// header records the policy for exactly that reason.
    /// </para>
    /// </remarks>
    private IReadOnlyList<PlayerId> NextSeating()
    {
        if (!SeatingPolicy.ReseatsBefore(RoundsPlayed))
        {
            return Seating;
        }

        var drawn = Players.ToArray();
        _random.Shuffle(drawn);
        return drawn;
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
