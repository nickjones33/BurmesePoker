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
            () => new RoundEngine(seats, agents, Stakes.Standard, order));
    }

    [Fact]
    public void AScriptedRoundReachesADeclarationAndSettles()
    {
        var order = Order(DealBuilder.ForPlayers(4).Give(0, WinningHand));
        var engine = Engine(order, Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })));

        var result = engine.Play();

        Assert.Equal(Alice, result.Winner);
        Assert.Equal(13, result.Melds.Sum(meld => meld.Count));

        // Nobody owns a money card, so only the flat round value moves: $5 from each loser.
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 15, [Bob] = -5, [Carol] = -5, [Dan] = -5 },
            result.Payouts);
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
        Assert.Equal(1, engine.Table.MoneyCards.Multiplier(claimed));
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 15, [Bob] = -5, [Carol] = -5, [Dan] = -5 },
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

        // Bob wins $5 a head but pays Alice $1 for the 7♦ he is holding.
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = -2, [Bob] = 14, [Carol] = -6, [Dan] = -6 },
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
    public void NobodyDeclaringRunsTheDrawPileOutAndSaysSo()
    {
        var engine = Engine(Order(DealBuilder.ForPlayers(4)), Passive(4));

        // Not caught here: gathering the discards and reshuffling is P9's (RULES.md §5).
        Assert.Throws<DeckExhaustedException>(() => engine.Play());
        Assert.Equal(0, engine.Table.DrawPileCount);
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
            () => new RoundEngine([Alice, Bob, Carol, Carol], agents, Stakes.Standard, order));
        Assert.Throws<ArgumentException>(
            () => new RoundEngine(FourPlayers, new Dictionary<PlayerId, IPlayerAgent> { [Alice] = agents[Alice] }, Stakes.Standard, order));
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

    private static RoundEngine Engine(
        IReadOnlyList<Card> order,
        IReadOnlyDictionary<PlayerId, IPlayerAgent> agents,
        IGameObserver? observer = null) =>
        new(
            [.. agents.Keys.OrderBy(player => player.Value)],
            agents,
            Stakes.Standard,
            order,
            observer: observer);

    private sealed class RogueAgent(Card discard) : IPlayerAgent
    {
        public TurnAction ChooseAction(TurnContext context) => TurnAction.DrawFromDeck;

        public Card ChooseDiscard(TurnContext context) => discard;

        public bool ClaimTurnedUpMoneyCard(TurnContext context) => false;

        public bool Declare(TurnContext context) => false;
    }
}
