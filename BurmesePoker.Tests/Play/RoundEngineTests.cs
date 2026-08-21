using System.Reflection;
using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Play;

/// <summary>
/// A whole round, played by scripted agents with no console anywhere near it.
/// </summary>
/// <remarks>
/// The deals come from <see cref="DealBuilder"/>, which fills every seat the test does not
/// name with cards that pay nothing — so a settlement expectation can be worked out by hand
/// and any money that moves is money the test asked for.
/// </remarks>
public class RoundEngineTests
{
    private static readonly PlayerId Alice = new(0);
    private static readonly PlayerId Bob = new(1);
    private static readonly PlayerId Carol = new(2);
    private static readonly PlayerId Dan = new(3);
    private static readonly PlayerId[] FourPlayers = [Alice, Bob, Carol, Dan];

    /// <summary>Thirteen cards that partition exactly: 2♥–7♥, 8♦9♦10♦, and all four kings.</summary>
    private static readonly string[] WinningHand =
        ["2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "KS", "KD"];

    [Fact]
    public void SetupDealsThirteenCardsEachAndTurnsUpTwoMoneyCards()
    {
        var order = Order(DealBuilder.ForPlayers(4));
        var engine = Engine(order, Passive(4));

        Assert.All(engine.Table.Seats, seat => Assert.Equal(13, seat.Hand.Count));
        Assert.Equal(2, engine.Table.TurnedUpOnTable.Count);

        // Bottom first, then top (RULES.md §3 step 4).
        Assert.Equal(order[^1], engine.Table.TurnedUpFromBottom);
        Assert.Equal(order[4 * 13], engine.Table.TurnedUpFromTop);

        // 108 - 52 dealt - 2 turned up.
        Assert.Equal(54, engine.Table.DrawPileCount);
    }

    [Fact]
    public void TheDealConfersOwnershipOnEveryCardDealt()
    {
        var engine = Engine(Order(DealBuilder.ForPlayers(4)), Passive(4));

        Assert.Equal(52, engine.Table.Ownership.Records.Count);

        foreach (var seat in engine.Table.Seats)
        {
            Assert.All(seat.Hand, card => Assert.Equal(seat.Id, engine.Table.Ownership.OwnerOf(card.Id)));
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    public void SetupRejectsATableThatCannotBeDealtMeaningfully(int players)
    {
        var order = Order(DealBuilder.ForPlayers(players));
        var seats = Enumerable.Range(0, players).Select(seat => new PlayerId(seat)).ToArray();
        var agents = seats.ToDictionary(seat => seat, _ => (IPlayerAgent)ScriptedPlayerAgent.Passive());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RoundEngine(seats, agents, Stakes.Standard, order, new Random(1)));
    }

    [Fact]
    public void AScriptedRoundReachesADeclarationAndSettles()
    {
        var order = Order(DealBuilder.ForPlayers(4).Give(0, WinningHand));
        var engine = Engine(order, Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })));

        var result = engine.Play();

        Assert.Equal(Alice, result.Winner);
        Assert.Equal(13, result.Melds.Sum(meld => meld.Count));

        // ⚠️ This said "nobody owns a money card" until P26, and the deal had not changed —
        // **jokers became permanent money cards** (RULES.md §4.1, rev 21). This deal gives Bob
        // the black joker and Dan the red one, so each collects $1 a head on top of the flat $5.
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 13, [Bob] = -3, [Carol] = -7, [Dan] = -3 },
            result.Payouts);

        var jokers = engine.Table.Ownership.Records
            .Select(record => engine.Table.AllCards.First(card => card.Id == record.Key))
            .Where(card => engine.Table.MoneyCards.Multiplier(card) > 0)
            .ToList();

        // Named rather than left implicit in the arithmetic above: two jokers, nothing else.
        Assert.Equal(2, jokers.Count);
        Assert.All(jokers, card => Assert.True(card.IsJoker));
    }

    [Fact]
    public void HandsAreThirteenBetweenTurnsAndFourteenDuringOne()
    {
        var alice = new ScriptedPlayerAgent(new ScriptedTurn(), Declaring("2C"));
        var observer = new RecordingObserver();
        var engine = Engine(LongerRound(), Agents(alice), observer);
        observer.Watch(engine.Table);

        engine.Play();

        Assert.Equal(2, alice.HandSizeBeforeTaking.Count);
        Assert.All(alice.HandSizeBeforeTaking, size => Assert.Equal(13, size));
        Assert.All(alice.HandSizeWhenDiscarding, size => Assert.Equal(14, size));
        Assert.All(observer.HandSizesBetweenTurns, size => Assert.Equal(13, size));
    }

    [Fact]
    public void TheRoundHoldsOneHundredAndEightDistinctCardsThroughout()
    {
        var observer = new RecordingObserver();
        var engine = Engine(LongerRound(), Agents(new ScriptedPlayerAgent(new ScriptedTurn(), Declaring("2C"))), observer);
        observer.Watch(engine.Table);

        engine.Play();

        Assert.NotEmpty(observer.CardsInPlay);
        Assert.All(observer.CardsInPlay, count => Assert.Equal(DeckBuilder.TotalCards, count));
        Assert.All(observer.DistinctCardsInPlay, count => Assert.Equal(DeckBuilder.TotalCards, count));
    }

    [Fact]
    public void ClaimingTheTurnedUpMoneyCardTakesTheCardOffTheTableAndGrantsNoOwnership()
    {
        // Alice claims the 3♣ on the opening turn, plays it as a spare, and goes out with a
        // king drawn back on her second turn.
        var order = Order(DealBuilder.ForPlayers(4)
            .Give(0, [.. WinningHand[..12], "2C"])
            .ThenDraw("2S", "3S", "4S", "KD"));

        var alice = new ScriptedPlayerAgent(
            new ScriptedTurn { Claim = true, Discard = "2C" },
            Declaring("3C"));

        var engine = Engine(order, Agents(alice));
        var claimed = engine.Table.TurnedUpFromTop;

        var result = engine.Play();

        Assert.True(engine.Table.TurnedUpFromTopClaimed);
        Assert.Equal([engine.Table.TurnedUpFromBottom], engine.Table.TurnedUpOnTable);
        Assert.Contains(claimed, engine.Table.SeatOf(Alice).Discards);
        Assert.Null(engine.Table.Ownership.OwnerOf(claimed.Id));

        // The claimed card is a money card, and it pays nobody: the table gave it, not the deck.
        // ⚠️ The rest of the movement is jokers, permanent money cards since RULES.md rev 21
        // (§4.1): Carol holds the red one and Dan the black one, and each collects $1 a head.
        // **Alice claimed a money card and still collects nothing for it**, which is the point.
        Assert.Equal(1, engine.Table.MoneyCards.Multiplier(claimed));
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 13, [Bob] = -7, [Carol] = -3, [Dan] = -3 },
            result.Payouts);

        Assert.Equal(DeckBuilder.TotalCards, engine.Table.AllCards.Select(card => card.Id).Distinct().Count());
    }

    [Fact]
    public void AMoneyCardDrawnThenDiscardedStillPaysTheDrawerWhoeverEndsUpHoldingIt()
    {
        // Alice draws the 7♦ and throws it away; Bob picks it up and wins with it.
        var order = Order(DealBuilder.ForPlayers(4)
            .Give(1, "5D", "6D", "8D", "9D", "2H", "3H", "4H", "5H", "KC", "KH", "KS", "KD", "2C")
            .ThenDraw("7D"));

        var bob = new ScriptedPlayerAgent(new ScriptedTurn
        {
            Action = TurnAction.TakeDiscard,
            Discard = "2C",
            Declare = true
        });

        var engine = Engine(order, Agents(ScriptedPlayerAgent.Passive(), bob));

        var result = engine.Play();

        var sevenOfDiamonds = engine.Table.SeatOf(Bob).Hand.Single(card => card.SameValueAs(Hands.Value("7D")));

        Assert.Equal(Bob, result.Winner);
        Assert.Equal(Alice, engine.Table.Ownership.OwnerOf(sevenOfDiamonds.Id));

        // Bob wins $5 a head but pays Alice $1 for the 7♦ he is holding. ⚠️ Alice also owns
        // the black joker and Dan the red one — permanent money cards since RULES.md rev 21
        // (§4.1) — so Alice collects on two cards and comes out level.
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 0, [Bob] = 12, [Carol] = -8, [Dan] = -4 },
            result.Payouts);
    }

    /// <summary>
    /// Only the deck confers ownership (RULES.md §4.4), so a round exercising all three ways
    /// of taking a card must end with exactly one record per dealt card and per blind draw —
    /// none for the card claimed off the table, and none for the one taken from a discard.
    /// </summary>
    [Fact]
    public void OnlyTheDealAndABlindDrawEverConferOwnership()
    {
        var order = Order(DealBuilder.ForPlayers(4)
            .Give(0, [.. WinningHand[..12], "2C"])
            .ThenDraw("2S", "3S", "KD"));

        var alice = new ScriptedPlayerAgent(
            new ScriptedTurn { Claim = true, Discard = "2C" },
            Declaring("3C"));
        var bob = new ScriptedPlayerAgent(new ScriptedTurn { Action = TurnAction.TakeDiscard });

        var observer = new RecordingObserver();
        var engine = Engine(order, Agents(alice, bob), observer);
        observer.Watch(engine.Table);

        engine.Play();

        var claimed = engine.Table.TurnedUpFromTop;
        var takenFromADiscard = engine.Table.Shoe.First(card => card.SameValueAs(Hands.Value("2C")));

        Assert.Contains(observer.Events, entry => entry.StartsWith($"{Bob} took"));
        Assert.Null(engine.Table.Ownership.OwnerOf(claimed.Id));
        Assert.Equal(Alice, engine.Table.Ownership.OwnerOf(takenFromADiscard.Id));

        var blindDraws = observer.Events.Count(entry => entry.Contains("drew"));
        Assert.Equal(52 + blindDraws, engine.Table.Ownership.Records.Count);
    }

    [Fact]
    public void RunningTheDrawPileOutGathersTheDiscardsShufflesThemAndPlayCarriesOn()
    {
        // Fifty-four cards to draw and one drawn per turn, so turn 55 is the first to find the
        // pile empty — and by then 54 cards have been discarded and none taken (RULES.md §5).
        var observer = new RecordingObserver();
        var engine = Engine(
            Order(DealBuilder.ForPlayers(4).Give(0, WinningHand)),
            Agents(WaitingToDeclare(turns: 15)),
            observer);

        observer.Watch(engine.Table);

        var result = engine.Play();

        Assert.Equal([54], observer.Reshuffles);
        Assert.Equal(Alice, result.Winner);
        Assert.Equal(57, result.Turns);

        // Three turns were played out of the replacement pile.
        Assert.Equal(51, engine.Table.DrawPileCount);

        // The gathering moves cards; it must not lose or copy one.
        Assert.All(observer.CardsInPlay, count => Assert.Equal(DeckBuilder.TotalCards, count));
        Assert.All(observer.DistinctCardsInPlay, count => Assert.Equal(DeckBuilder.TotalCards, count));
    }

    [Fact]
    public void TheTurnedUpCardsAreNotGatheredIntoTheReshuffle()
    {
        // They stay out of play for the rest of the round — RULES.md §9 #4's recommendation,
        // taken as the default. Only the discards are gathered (§5).
        var observer = new RecordingObserver();
        var engine = Engine(
            Order(DealBuilder.ForPlayers(4).Give(0, WinningHand)),
            Agents(WaitingToDeclare(turns: 15)),
            observer);

        observer.Watch(engine.Table);
        engine.Play();

        Assert.Equal(2, engine.Table.TurnedUpOnTable.Count);
        Assert.Contains(engine.Table.TurnedUpFromTop, engine.Table.TurnedUpOnTable);
        Assert.Contains(engine.Table.TurnedUpFromBottom, engine.Table.TurnedUpOnTable);
    }

    [Fact]
    public void AMoneyCardDiscardedAndRedrawnAfterAReshuffleStillPaysItsFirstOwner()
    {
        // Bob is dealt the 7♦ — a permanent money card (RULES.md §4.1) — and throws it away on
        // his first turn. It lies in his discard pile until the draw pile runs out, is gathered
        // and shuffled back in, and comes off the deck again for somebody else. Ownership is
        // write-once and does not follow the card: first acquisition wins (RULES.md §5).
        var observer = new RecordingObserver();
        var engine = Engine(
            Order(DealBuilder.ForPlayers(4).Give(0, WinningHand).Give(1, "7D")),
            Agents(WaitingToDeclare(turns: 26), new ScriptedPlayerAgent(new ScriptedTurn { Discard = "7D" })),
            observer,
            seed: ReshuffleSeed);

        observer.Watch(engine.Table);
        var seven = engine.Table.SeatOf(Bob).Hand.Single(card => card.SameValueAs(Hands.Value("7D")));

        var result = engine.Play();

        Assert.Single(observer.Reshuffles);

        var redrawn = observer.DrawsAfterFirstReshuffle.Single(draw => draw.Card.Id == seven.Id);
        Assert.NotEqual(Bob, redrawn.Player);
        Assert.Equal(Bob, engine.Table.Ownership.OwnerOf(seven.Id));

        // And nothing else that was owned before the reshuffle changed hands either.
        Assert.All(
            observer.OwnersAtFirstReshuffle,
            owned => Assert.Equal(owned.Value, engine.Table.Ownership.OwnerOf(owned.Key)));

        Assert.Equal(0, result.Payouts.Values.Sum());
    }

    [Fact]
    public void TheResultSaysHowManyTurnsTheRoundRan()
    {
        // Free where the engine already counts, and the most-wanted round statistic
        // (BUILD-PLAN §3.8).
        var immediate = Engine(
            Order(DealBuilder.ForPlayers(4).Give(0, WinningHand)),
            Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })));

        Assert.Equal(1, immediate.Play().Turns);

        var lap = Engine(LongerRound(), Agents(new ScriptedPlayerAgent(new ScriptedTurn(), Declaring("2C"))));

        Assert.Equal(5, lap.Play().Turns);
    }

    [Fact]
    public void AnAgentCannotDiscardACardItIsNotHolding()
    {
        var order = Order(DealBuilder.ForPlayers(4));
        var agents = Agents(new RogueAgent(Card.Ranked(new CardId(0), Rank.Ace, Suit.Spades)));

        Assert.Throws<InvalidOperationException>(() => Engine(order, agents).Play());
    }

    [Fact]
    public void ARoundIsPlayedOnce()
    {
        var engine = Engine(
            Order(DealBuilder.ForPlayers(4).Give(0, WinningHand)),
            Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })));

        engine.Play();

        Assert.Throws<InvalidOperationException>(() => engine.Play());
    }

    [Fact]
    public void ARoundIsDealtFromTheWholeShoeAndNothingElse()
    {
        var order = Order(DealBuilder.ForPlayers(4));

        Assert.Throws<ArgumentException>(() => Engine([.. order.Take(107)], Passive(4)));
        Assert.Throws<ArgumentException>(() => Engine([.. order.Take(107), order[0]], Passive(4)));
    }

    [Fact]
    public void EveryPlayerNeedsAnAgentAndNobodySitsTwice()
    {
        var order = Order(DealBuilder.ForPlayers(4));
        var agents = FourPlayers.ToDictionary(player => player, _ => (IPlayerAgent)ScriptedPlayerAgent.Passive());

        Assert.Throws<ArgumentException>(
            () => new RoundEngine([Alice, Bob, Carol, Carol], agents, Stakes.Standard, order, new Random(1)));
        Assert.Throws<ArgumentException>(
            () => new RoundEngine(FourPlayers, new Dictionary<PlayerId, IPlayerAgent> { [Alice] = agents[Alice] }, Stakes.Standard, order, new Random(1)));
    }

    [Fact]
    public void AShuffledRoundIsSetUpTheSameWay()
    {
        var agents = FourPlayers.ToDictionary(player => player, _ => (IPlayerAgent)ScriptedPlayerAgent.Passive());

        var engine = RoundEngine.Shuffled(FourPlayers, agents, Stakes.Standard, new Random(1));

        Assert.All(engine.Table.Seats, seat => Assert.Equal(13, seat.Hand.Count));
        Assert.Equal(54, engine.Table.DrawPileCount);
        Assert.Equal(DeckBuilder.TotalCards, engine.Table.AllCards.Select(card => card.Id).Distinct().Count());
    }

    [Fact]
    public void TheObserverIsToldWhatHappenedInOrder()
    {
        var observer = new RecordingObserver();
        var engine = Engine(
            Order(DealBuilder.ForPlayers(4).Give(0, WinningHand)),
            Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })),
            observer);

        var result = engine.Play();

        Assert.Equal(5, observer.Events.Count);
        Assert.StartsWith("round 1 started", observer.Events[0]);
        Assert.Contains("drew", observer.Events[1]);
        Assert.Contains("discarded", observer.Events[2]);
        Assert.Contains("declared", observer.Events[3]);
        Assert.Same(result, observer.Settled);
    }

    /// <summary>
    /// Play is fully concealed (RULES.md §6.3), so the type a player is handed must offer no
    /// route to anybody else's cards — not the table, not a seat, not the ownership record,
    /// which would say which money cards an opponent was dealt.
    /// </summary>
    [Fact]
    public void TurnContextOffersNoRouteToAnotherPlayersCards()
    {
        Type[] forbidden = [typeof(TableState), typeof(PlayerState), typeof(CardOwnership)];

        var leaks = typeof(TurnContext)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(member => member switch
            {
                PropertyInfo property => property.PropertyType,
                MethodInfo method => method.ReturnType,
                FieldInfo field => field.FieldType,
                _ => typeof(void)
            })
            .Where(forbidden.Contains)
            .ToList();

        Assert.Empty(leaks);
    }

    /// <summary>
    /// The seed the reshuffle test is pinned to. Any seed reshuffles, but this one puts the
    /// 7♦ back into somebody else's hands, which is the case the rule is about.
    /// </summary>
    private const int ReshuffleSeed = 2;

    /// <summary>
    /// A seat holding a winning hand that declines to declare until its <paramref name="turns"/>th
    /// turn — the way to make a round run long enough to empty the draw pile.
    /// </summary>
    private static ScriptedPlayerAgent WaitingToDeclare(int turns) =>
        new([.. Enumerable.Repeat(new ScriptedTurn(), turns - 1), new ScriptedTurn { Declare = true }]);

    /// <summary>Alice needs one more turn, so the round runs a full lap of the table first.</summary>
    private static IReadOnlyList<Card> LongerRound() =>
        Order(DealBuilder.ForPlayers(4)
            .Give(0, [.. WinningHand[..12], "2C"])
            .ThenDraw("2S", "3S", "4S", "5S", "KD"));

    private static ScriptedTurn Declaring(string discard) =>
        new() { Discard = discard, Declare = true };

    private static IReadOnlyList<Card> Order(DealBuilder deal) =>
        deal.TurnUpFromTop("3C").TurnUpFromBottom("4C").Build();

    private static IReadOnlyDictionary<PlayerId, IPlayerAgent> Passive(int players) =>
        Enumerable.Range(0, players)
            .ToDictionary(seat => new PlayerId(seat), _ => (IPlayerAgent)ScriptedPlayerAgent.Passive());

    /// <summary>The named agents in seating order; the rest of the table plays passively.</summary>
    private static IReadOnlyDictionary<PlayerId, IPlayerAgent> Agents(params IPlayerAgent[] agents) =>
        FourPlayers.ToDictionary(
            player => player,
            player => player.Value < agents.Length ? agents[player.Value] : ScriptedPlayerAgent.Passive());

    /// <remarks>
    /// The seed matters only if the round runs the draw pile out and reshuffles (RULES.md §5);
    /// tests that do give their own.
    /// </remarks>
    private static RoundEngine Engine(
        IReadOnlyList<Card> order,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        IGameObserver? observer = null,
        int seed = 1) =>
        new(
            [.. agents.Keys.OrderBy(player => player.Value)],
            agents,
            Stakes.Standard,
            order,
            new Random(seed),
            observer: observer);

    private sealed class RogueAgent(Card discard) : IPlayerAgent
    {
        public TurnAction ChooseAction(TurnContext context) => TurnAction.DrawFromDeck;

        public Card ChooseDiscard(TurnContext context) => discard;

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => false;

        public bool Declare(TurnContext context) => false;
    }
}
