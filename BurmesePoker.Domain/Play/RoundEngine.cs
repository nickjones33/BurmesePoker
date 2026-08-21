using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;

namespace BurmesePoker.Domain.Play;

/// <summary>
/// Plays one round: deal, turn up the money cards, take turns until somebody declares, then
/// settle (RULES.md §3, §5, §7.1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here prints and nothing here reads input.</b> Every decision is asked of an
/// <see cref="IPlayerAgent"/> and every event is told to an <see cref="IGameObserver"/>, so
/// a whole round is drivable from a test with no console involved at all.
/// </para>
/// <para>
/// <b>The round is dealt from a draw order, not from a shuffle.</b> The constructor takes the
/// 108 cards in the order they will leave the deck, which makes a round reproducible; use
/// <see cref="Shuffled"/> for a real game. Setup consumes that order exactly as RULES.md §3
/// specifies: the first 13 × <em>n</em> cards are dealt one at a time around the table, then
/// the <b>last</b> card is turned up from the bottom, then the next unread card from the top.
/// </para>
/// <para>
/// <b>Seating is taken as given.</b> RULES.md §3 step 2 randomises it, but a round engine
/// that reshuffled its own seating could not be scripted; whoever sets the match up passes
/// the players in seating order, which is also the order settlement is handed.
/// </para>
/// <para>
/// <b>Deck exhaustion is handled at the moment of drawing</b> (RULES.md §5): if the draw pile
/// is empty when somebody draws, every discard pile is gathered, shuffled, and becomes the new
/// draw pile. It cannot be handled around <see cref="Play"/> instead, because the exception
/// would unwind from the middle of a turn with no resume point.
/// <see cref="DeckExhaustedException"/> therefore now means what it should — there is nothing
/// left anywhere, in the deck or in any discard pile.
/// </para>
/// </remarks>
public sealed class RoundEngine
{
    /// <summary>Cards dealt to each player (RULES.md §2).</summary>
    public const int HandSize = 13;

    /// <summary>Fewest players a round may be dealt for.</summary>
    public const int MinimumPlayers = 4;

    /// <summary>
    /// Most players a round may be dealt for. Six is where the arithmetic runs out: at seven
    /// the draw pile leaves about two draws each and the game is barely playable
    /// (RULES.md §2.1).
    /// </summary>
    public const int MaximumPlayers = 6;

    private readonly Dictionary<PlayerId, IPlayerAgent> _agents;
    private readonly IGameObserver _observer;
    private readonly Random _random;
    private readonly int _round;

    /// <param name="players">The seating order. Between four and six, no duplicates.</param>
    /// <param name="agents">One agent per player.</param>
    /// <param name="stakes">What the round is played for.</param>
    /// <param name="drawOrder">
    /// The whole 108-card shoe in the order it will leave the deck, top first. Must be a
    /// permutation of <see cref="DeckBuilder.BuildTwoDecks"/> — every card exactly once.
    /// </param>
    /// <param name="random">
    /// The randomness the <em>round</em> needs, which is the reshuffle at deck exhaustion
    /// (RULES.md §5). Required rather than defaulted, because a round is reproducible from its
    /// draw order and its seed together, and a domain type that reached for
    /// <see cref="Random.Shared"/> would break that in silence (BUILD-PLAN §3.7).
    /// </param>
    /// <param name="round">Which round of the match this is, for narration.</param>
    /// <param name="observer">Narration sink. Optional; nothing is required of it.</param>
    public RoundEngine(
        IReadOnlyList<PlayerId> players,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        Stakes stakes,
        IReadOnlyList<Card> drawOrder,
        Random random,
        int round = 1,
        IGameObserver? observer = null)
    {
        ArgumentNullException.ThrowIfNull(stakes);
        ArgumentNullException.ThrowIfNull(drawOrder);
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfLessThan(round, 1);

        _agents = RequireAPlayableTable(players, agents);
        _random = random;
        _round = round;
        _observer = observer ?? new SilentObserver();

        var shoe = DeckBuilder.BuildTwoDecks();
        RequirePermutationOfTheShoe(drawOrder, shoe);

        Table = Deal(players, stakes, drawOrder, shoe);
        _observer.RoundStarted(_round, Table.TurnedUpOnTable);
    }

    /// <summary>Sets a round up from a freshly shuffled shoe (RULES.md §3 step 1).</summary>
    public static RoundEngine Shuffled(
        IReadOnlyList<PlayerId> players,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        Stakes stakes,
        Random random,
        int round = 1,
        IGameObserver? observer = null)
    {
        ArgumentNullException.ThrowIfNull(random);

        var deck = Deck.TwoDecks();
        deck.Shuffle(random);

        return new RoundEngine(players, agents, stakes, [.. deck.Cards], random, round, observer);
    }

    /// <summary>
    /// Checks that the table can be dealt at all, and takes a private copy of the agents.
    /// Shared with <see cref="MatchEngine"/> so the rule is stated once.
    /// </summary>
    internal static Dictionary<PlayerId, IPlayerAgent> RequireAPlayableTable(
        IReadOnlyList<PlayerId> players,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(agents);

        if (players.Count is < MinimumPlayers or > MaximumPlayers)
        {
            throw new ArgumentOutOfRangeException(
                nameof(players),
                players.Count,
                $"A round is for {MinimumPlayers} to {MaximumPlayers} players (RULES.md §2.1).");
        }

        if (players.Distinct().Count() != players.Count)
        {
            throw new ArgumentException("The same player is seated twice.", nameof(players));
        }

        var seated = new Dictionary<PlayerId, IPlayerAgent>(players.Count);

        foreach (var player in players)
        {
            if (!agents.TryGetValue(player, out var agent) || agent is null)
            {
                throw new ArgumentException($"{player} has no agent.", nameof(agents));
            }

            seated[player] = agent;
        }

        return seated;
    }

    /// <summary>The table as it stands. Readable at any point, including before the first turn.</summary>
    public TableState Table { get; }

    /// <summary>How the round ended, or null while it is still being played.</summary>
    public RoundResult? Result { get; private set; }

    /// <summary>
    /// Plays turns until somebody declares, and settles.
    /// </summary>
    /// <exception cref="DeckExhaustedException">
    /// There was nothing left to draw anywhere — neither in the draw pile nor in any discard
    /// pile, so not even the reshuffle of RULES.md §5 could carry on. A real end state, and
    /// rarer than the reshuffle that prevents it.
    /// </exception>
    public RoundResult Play()
    {
        if (Result is not null)
        {
            throw new InvalidOperationException("This round has already been played.");
        }

        var players = Table.Players;

        for (var turn = 1; ; turn++)
        {
            var seat = Table.SeatOf(players[(turn - 1) % players.Count]);
            var previous = Table.SeatOf(players[(turn - 1 + players.Count - 1) % players.Count]);

            if (TakeTurn(seat, previous, turn) is { } result)
            {
                Result = result;
                return result;
            }
        }
    }

    private RoundResult? TakeTurn(PlayerState seat, PlayerState previous, int turn)
    {
        var agent = _agents[seat.Id];
        var available = turn == 1 ? null : previous.TopDiscard;
        var canClaim = turn == 1 && !Table.TurnedUpFromTopClaimed;

        // 1. Take exactly one card (RULES.md §5), by one of three routes. Only the blind
        //    draw comes from the deck, so only the blind draw confers ownership (§4.4).
        var context = new TurnContext(Table, seat, _round, turn, available, canClaim, taken: null);
        var taken = TakeCard(agent, seat, previous, context);

        // 2. Discard (RULES.md §5), from the legal cards only — a rank the next seat has taken in
        //    the open is not a move this turn offers (§5.1). Ownership is untouched by it (§4.4).
        context = new TurnContext(Table, seat, _round, turn, available, canClaim, taken);
        var discarded = seat.Discard(RequireALegalDiscard(context, agent.ChooseDiscard(context)));
        _observer.PlayerDiscarded(seat.Id, discarded);

        // 3. Only then may the hand be laid down — the discard comes first and the reveal
        //    follows it (RULES.md §7.1).
        context = new TurnContext(Table, seat, _round, turn, available, canClaim, taken);

        if (!HandEvaluator.TryFindCover(seat.Hand, Table.Rules, out var melds) || !agent.Declare(context))
        {
            return null;
        }

        _observer.PlayerDeclared(seat.Id, melds);

        var payouts = Settlement.ForRound(
            Table.Players, seat.Id, Table.Stakes, Table.MoneyCards, Table.Ownership, Table.Shoe);

        var result = new RoundResult(_round, seat.Id, melds, payouts, turn);
        _observer.RoundSettled(result);
        return result;
    }

    /// <remarks>
    /// The card joins the hand <b>before</b> the event is narrated, so an observer never sees
    /// it in flight: at every moment an observer can look, the round holds all 108 cards.
    /// </remarks>
    private Card TakeCard(IPlayerAgent agent, PlayerState seat, PlayerState previous, TurnContext context)
    {
        if (context.CanClaimTurnedUpMoneyCard && agent.ClaimTurnedUpMoneyCard(context))
        {
            // The actual card leaves the table — it is never copied — and the deck did not
            // give it to anybody, so no ownership is recorded (RULES.md §4.5).
            var claimed = Table.ClaimTurnedUpFromTop();
            seat.Take(claimed);

            // Taken where the table could see it, so it closes that rank to whoever feeds this
            // seat (RULES.md §5.1, §4.5).
            seat.TookInTheOpen(claimed);
            _observer.MoneyCardClaimed(seat.Id, claimed);
            return claimed;
        }

        if (context.AvailableDiscard is not null && ChooseAction(agent, context) == TurnAction.TakeDiscard)
        {
            var pickedUp = previous.TakeTopDiscard();
            seat.Take(pickedUp);

            // The take everyone watched: it announces what this seat is collecting, and closes that
            // rank to the seat that discards to them (RULES.md §5.1).
            seat.TookInTheOpen(pickedUp);
            _observer.PlayerTookDiscard(seat.Id, pickedUp);
            return pickedUp;
        }

        if (Table.DrawPile.IsEmpty)
        {
            // The draw pile is out, so the discards become the new one (RULES.md §5). Here
            // rather than around Play(), because there is no resume point mid-turn.
            _observer.DiscardsReshuffled(Table.ReplaceDrawPileWithTheDiscards(_random));
        }

        var drawn = Table.DrawPile.DrawFromTop();
        seat.Take(drawn);

        // First acquisition wins (RULES.md §5): after a reshuffle a card can leave the deck a
        // second time, and its original owner keeps it.
        Table.Ownership.TryRecordFromDeck(drawn.Id, seat.Id);
        _observer.PlayerDrew(seat.Id, drawn);
        return drawn;
    }

    /// <summary>
    /// The card a player says they are throwing, if the turn actually offered it (RULES.md §5.1).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>This is a guard against a broken agent, not the enforcement.</b> The ban is enforced
    /// by construction — <see cref="TurnContext.LegalDiscards"/> is what every player picks from,
    /// so a banned card is never presented and the question *"what happens if somebody throws one
    /// anyway?"* has no answer at a real table. What is left is the same class of mistake as naming
    /// a card you are not holding, which <see cref="PlayerState.Discard"/> already refuses: an
    /// answer that did not come from the choice offered.
    /// </remarks>
    private static Card RequireALegalDiscard(TurnContext context, Card chosen)
    {
        foreach (var legal in context.LegalDiscards)
        {
            if (legal.Id == chosen.Id)
            {
                return chosen;
            }
        }

        throw new InvalidOperationException(
            $"{context.Player} cannot throw {chosen}: the seat they discard to has taken that rank "
            + "in the open and has not thrown it back (RULES.md §5.1).");
    }

    private static TurnAction ChooseAction(IPlayerAgent agent, TurnContext context)
    {
        var action = agent.ChooseAction(context);

        return Enum.IsDefined(action)
            ? action
            : throw new InvalidOperationException($"{context.Player} chose no known action.");
    }

    private static TableState Deal(
        IReadOnlyList<PlayerId> players,
        Stakes stakes,
        IReadOnlyList<Card> drawOrder,
        IReadOnlyList<Card> shoe)
    {
        var deck = new Deck(drawOrder);
        var hands = players.ToDictionary(player => player, _ => new List<Card>(HandSize));

        // One card at a time around the table (RULES.md §3 step 3).
        for (var round = 0; round < HandSize; round++)
        {
            foreach (var player in players)
            {
                hands[player].Add(deck.DrawFromTop());
            }
        }

        // Bottom first, then top (RULES.md §3 step 4) — so the money cards are turned up
        // after the deal, and a player may already hold a copy of one.
        var fromBottom = deck.DrawFromBottom();
        var fromTop = deck.DrawFromTop();

        var table = new TableState(players, stakes, deck, shoe, fromBottom, fromTop);

        foreach (var (player, hand) in hands)
        {
            var seat = table.SeatOf(player);

            foreach (var card in hand)
            {
                seat.Take(card);

                // The deal confers ownership — confirmed, and the whole reason most money
                // cards are spoken for before anybody acts (RULES.md §4.4).
                table.Ownership.RecordFromDeck(card.Id, player);
            }
        }

        return table;
    }

    private static void RequirePermutationOfTheShoe(IReadOnlyList<Card> drawOrder, IReadOnlyList<Card> shoe)
    {
        if (drawOrder.Count != shoe.Count)
        {
            throw new ArgumentException(
                $"A round is dealt from the whole shoe: {shoe.Count} cards, not {drawOrder.Count}.",
                nameof(drawOrder));
        }

        var seen = new HashSet<CardId>(drawOrder.Count);

        foreach (var card in drawOrder)
        {
            if (!seen.Add(card.Id))
            {
                throw new ArgumentException($"{card} appears twice in the draw order.", nameof(drawOrder));
            }

            if (card.Id.Value < 0 || card.Id.Value >= shoe.Count || !card.SameValueAs(shoe[card.Id.Value]))
            {
                throw new ArgumentException($"{card} is not a card of the shoe.", nameof(drawOrder));
            }
        }
    }

    private sealed class SilentObserver : IGameObserver;
}
