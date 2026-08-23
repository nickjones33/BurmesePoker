using System.Reflection;
using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
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
        // the black joker and Dan the red one, so each collects $1 a head on top of the round
        // payment.
        //
        // 🔥 And the round payment doubled at P33: WinningHand is 2♥–7♥, 8♦9♦10♦ and four
        // kings — **no joker anywhere in the thirteen** — so §7.3 pays ×2 at this four-handed
        // table and each loser pays $10 rather than $5. The hand was jokerless the whole time;
        // it is what the hand is *worth* that changed.
        Assert.True(result.Jokerless);
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 28, [Bob] = -8, [Carol] = -12, [Dan] = -8 },
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
    public void AJokerAnywhereInTheDeclaredThirteenPaysFlat()
    {
        // RULES.md §7.3, the discriminating case, played out: 2♥–7♥ and 8♦9♦10♦ are clean
        // series and the fourth meld is a **set** of kings with two jokers standing in. Every
        // series here is clean — rev 25's withdrawn reading would have paid the bonus — and the
        // thirteen hold jokers, so rev 26 pays the flat $5 a loser.
        var jokerInASet =
            new[] { "2H", "3H", "4H", "5H", "6H", "7H", "8D", "9D", "10D", "KC", "KH", "RJ", "BJ" };
        var order = Order(DealBuilder.ForPlayers(4).Give(0, jokerInASet));
        var engine = Engine(order, Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })));

        var result = engine.Play();

        Assert.Equal(Alice, result.Winner);
        Assert.Equal(13, result.Melds.Sum(meld => meld.Count));
        Assert.False(result.Jokerless);

        // Alice owns both jokers, so the side bet pays her $1 a head twice; the round pays the
        // flat $15. Nobody else owns a paying card — this deal took both jokers out of the
        // filler that would otherwise have scattered them.
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 15 + 6, [Bob] = -7, [Carol] = -7, [Dan] = -7 },
            result.Payouts);
    }

    [Fact]
    public void FiveHandedAJokerlessDeclarationPaysTripleTheRoundValue()
    {
        // RULES.md §7.3 and §9 #35's answer: at five seats §7.1.1 asks nothing of the partition
        // and the bonus is the only thing cleanliness is worth — ×3, so $15 a loser.
        var eve = new PlayerId(4);
        var players = new[] { Alice, Bob, Carol, Dan, eve };
        var order = DealBuilder.ForPlayers(5)
            .Give(0, WinningHand)
            .Give(1, "RJ", "BJ")
            .TurnUpFromTop("3C")
            .TurnUpFromBottom("4C")
            .Build();

        var agents = players.ToDictionary(
            player => player,
            player => player == Alice
                ? (IPlayerAgent)new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })
                : ScriptedPlayerAgent.Passive());

        var result = new RoundEngine(players, agents, Stakes.Standard, order, new Random(1)).Play();

        Assert.NotNull(result);
        Assert.Equal(Alice, result.Winner);
        Assert.True(result.Jokerless);

        // $15 from each of four losers, and Bob's two jokers pay him $1 a head from the other
        // four on top of that.
        Assert.Equal(
            new Dictionary<PlayerId, int>
            {
                [Alice] = 60 - 2, [Bob] = -15 + 8, [Carol] = -17, [Dan] = -17, [eve] = -17
            },
            result.Payouts);
    }

    [Fact]
    public void AJackpotRoundCarriesItsOwnerOnTheResult()
    {
        // RULES.md §4.1's ×5, played out rather than settled by hand: the turn-up is the
        // 7♦/A♠ pair and the deck gave Bob both partners. The pair does not turn up by
        // accident at any usable rate (about one round in 1,444), which is why the round is
        // constructed — and the fact is asserted off the result, because a watcher cannot
        // compute it: ownership is partly private until settlement (BUILD-PLAN P42).
        var order = DealBuilder.ForPlayers(4)
            .Give(0, WinningHand)
            // Both partners and all four jokers, so every paying card is Bob's and the
            // arithmetic below has no filler joker hiding in it.
            .Give(1, "7D", "AS", "RJ", "RJ", "BJ", "BJ")
            .TurnUpFromTop("7D")
            .TurnUpFromBottom("AS")
            .Build();
        var engine = Engine(order, Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })));

        var result = engine.Play();

        Assert.Equal(Bob, result.JackpotOwner);

        // The round: WinningHand is jokerless, ×2 at four seats, $10 a loser. The side bet:
        // Bob's partners pay ×5 apiece instead of ×3 — (5 + 5 + 4 jokers) × $1 = $14 a head.
        Assert.Equal(
            new Dictionary<PlayerId, int>
            {
                [Alice] = 30 - 14, [Bob] = -10 + 42, [Carol] = -24, [Dan] = -24
            },
            result.Payouts);
    }

    [Fact]
    public void TheSamePairSplitBetweenTwoPlayersCarriesNoJackpotOwner()
    {
        // The same turn-up with the partners split is two ×3s and no jackpot (RULES.md §4.1),
        // and the result says so by carrying nothing — the ordinary case for every round that
        // ever settles at a real table.
        var order = DealBuilder.ForPlayers(4)
            .Give(0, WinningHand)
            .Give(1, "7D")
            .Give(2, "AS")
            .TurnUpFromTop("7D")
            .TurnUpFromBottom("AS")
            .Build();
        var engine = Engine(order, Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })));

        Assert.Null(engine.Play().JackpotOwner);
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
        // 🔥 The round half doubled at P33: Alice declares 2♥–7♥, 8♦9♦10♦ and four kings with
        // no joker in the thirteen, so §7.3 pays ×2 at four seats — $10 a loser.
        Assert.Equal(1, engine.Table.MoneyCards.Multiplier(claimed));
        Assert.True(result.Jokerless);
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 28, [Bob] = -12, [Carol] = -8, [Dan] = -8 },
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

        // Bob wins $10 a head but pays Alice $1 for the 7♦ he is holding. ⚠️ Alice also owns
        // the black joker and Dan the red one — permanent money cards since RULES.md rev 21
        // (§4.1) — so Alice collects on two cards.
        // 🔥 $10 and not $5 since P33: Bob's thirteen are 5♦–9♦, 2♥–5♥ and four kings, with no
        // joker among them, so §7.3 doubles the round payment at this four-handed table. ⚠️ It
        // is what takes Alice from level to $5 down — the bonus is paid by every loser,
        // including one who is collecting on two money cards.
        Assert.True(result.Jokerless);
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = -5, [Bob] = 27, [Carol] = -13, [Dan] = -9 },
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
            // ⚠️ Not a 3: Alice claims the 3♣ in the open on turn 1, which closes threes to Dan
            // — the seat that discards to her — so the 3♠ Dan used to draw and throw straight back
            // stopped being a legal move when §5.1 was built (P27). A passive seat throws back
            // what it drew, and that is now something the rules can refuse.
            .ThenDraw("2S", "4S", "KD"));

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

    // ---------------------------------------------------------------------------------------
    // RULES.md §7.4 — the deal bonus. Packet P35.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void AThirteenThatAlreadyWinsIsLaidDownBeforeAnybodyDrawsAndPaysDouble()
    {
        // §7.4: "if you win on an initial deal you get double payout." Alice is dealt the
        // winning thirteen and says yes to the one question a round now asks before it starts.
        var order = Order(DealBuilder.ForPlayers(4).Give(0, WinningHand));
        var alice = new ScriptedPlayerAgent { DeclaresOnTheDeal = true };
        var observer = new RecordingObserver();
        var engine = Engine(order, Agents(alice), observer);

        var result = engine.Play();

        Assert.Equal(Alice, result.Winner);
        Assert.True(result.Win.FromTheInitialDeal);

        // 🔥 A round with no turn in it — the first shape this engine has had that contains
        // none. Nobody took a card, so nobody discarded one either.
        Assert.Equal(0, result.Turns);
        Assert.All(engine.Table.Seats, seat => Assert.Equal(13, seat.Hand.Count));
        Assert.All(engine.Table.Seats, seat => Assert.Empty(seat.Discards));
        Assert.Equal(54, engine.Table.DrawPileCount);

        // The thirteen are jokerless as well, so §7.3's ×2 and §7.4's ×2 multiply (§9 #39):
        // $20 a loser at this four-handed table. Bob and Dan still hold a joker each, which
        // pays them $1 a head as it does in every other round of this file.
        Assert.True(result.Jokerless);
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 58, [Bob] = -18, [Carol] = -22, [Dan] = -18 },
            result.Payouts);
    }

    /// <summary>
    /// ⚠️ <b>RULES.md §9 #38's recorded default, fenced: the bonus is <i>the dealt thirteen
    /// alone</i>.</b> The competing reading is the winner's <b>first turn</b>, after one take and
    /// one discard — which is a far commoner event, and would need no engine path at all.
    /// <b>The day an expert answers, this is the test that fails.</b>
    /// </summary>
    [Fact]
    public void TheDealBonusIsTheDealtThirteenAloneUntilTheExpertSaysOtherwise()
    {
        // Alice is dealt twelve of the winning thirteen and completes it on her first turn by
        // drawing the king and throwing the spare. That is a win on turn 1 and not on the deal.
        var order = Order(DealBuilder.ForPlayers(4)
            .Give(0, [.. WinningHand[..12], "2C"])
            .ThenDraw("KD"));

        var alice = new ScriptedPlayerAgent(Declaring("2C")) { DeclaresOnTheDeal = true };

        var result = Engine(order, Agents(alice)).Play();

        Assert.Equal(Alice, result.Winner);
        Assert.Equal(1, result.Turns);
        Assert.False(result.Win.FromTheInitialDeal);

        // ×2 for the jokerless thirteen and nothing else: $10 a loser, not $20.
        Assert.True(result.Jokerless);
        Assert.Equal(
            10, Settlement.RoundPayment(Stakes.Standard, TableRules.For(4), result.Win));
    }

    [Fact]
    public void ASeatDealtAWinningThirteenMayDeclineItAndPlayTheRoundOut()
    {
        // Declaring is a choice (§7.1), and there is no §5.1 exception in play before the first
        // turn to make the move itself the declaration — nobody has discarded anything.
        var order = Order(DealBuilder.ForPlayers(4).Give(0, WinningHand));
        var alice = new ScriptedPlayerAgent(new ScriptedTurn { Declare = true });

        var result = Engine(order, Agents(alice)).Play();

        Assert.Equal(Alice, result.Winner);
        Assert.Equal(1, result.Turns);
        Assert.False(result.Win.FromTheInitialDeal);
    }

    /// <summary>
    /// ⚠️ <b>RULES.md §9 #48's recorded default, fenced</b>: two seats can be dealt winning
    /// thirteens at once, and the one earlier in <b>turn order</b> takes the round — the order
    /// the table would have found out in. Nobody has been asked this.
    /// </summary>
    [Fact]
    public void WhenTwoSeatsAreDealtAWinningThirteenTheEarlierInTurnOrderTakesIt()
    {
        var order = Order(DealBuilder.ForPlayers(4)
            .Give(1, WinningHand)
            .Give(2, "5S", "6S", "7S", "8S", "9S", "10S", "2D", "3D", "4D", "QC", "QH", "QS", "QD"));

        var bob = new ScriptedPlayerAgent { DeclaresOnTheDeal = true };
        var carol = new ScriptedPlayerAgent { DeclaresOnTheDeal = true };

        var engine = Engine(order, Agents(ScriptedPlayerAgent.Passive(), bob, carol));

        // Both hands really do win — the test would be vacuous if Carol's did not.
        Assert.True(HandEvaluator.IsWinning(engine.Table.SeatOf(Carol).Hand, engine.Table.Rules));

        var result = engine.Play();

        Assert.Equal(Bob, result.Winner);
        Assert.Equal(0, result.Turns);
    }

    // ---------------------------------------------------------------------------------------
    // RULES.md §7.5 — the feeding blame, as a round is told about it. Packet P35.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ARoundToldItIsAThirdConsecutiveWinBillsTheSeatAboveTheWinner()
    {
        // The streak is handed in — a round has no memory of any other round (RULES.md §7.5).
        // Alice is seat 0, so the seat above her is Dan, who discards into her.
        var order = Order(DealBuilder.ForPlayers(4).Give(0, WinningHand));
        var agents = Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true }));

        var result = new RoundEngine(
            FourPlayers, agents, Stakes.Standard, order, new Random(1),
            streak: new WinStreak(Alice, 2)).Play();

        Assert.Equal(Alice, result.Winner);
        Assert.True(result.Win.ThirdConsecutiveWin);

        // The round half: $10 a head jokerless at four seats, ×3 losers = $30, all of it from
        // Dan. The side bet is unmoved — Bob and Dan hold a joker each (§9 #44).
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 30 - 2, [Bob] = 2, [Carol] = -2, [Dan] = -28 },
            result.Payouts);
    }

    /// <summary>
    /// ⚠️ <b>RULES.md §9 #46's recorded default, fenced</b>: the seats can move between the
    /// rounds of a streak — by a house policy (P36) or because the table agreed to it (P37) — so
    /// <b>which</b> seating names the payer is a live question. The default is <b>the round
    /// being settled</b>, which is what settling a round from its own state already means.
    /// <b>The day an expert says the run's first seating decides, this is the test that
    /// fails.</b>
    /// </summary>
    [Fact]
    public void TheSeatBlamedIsTakenFromTheSeatingOfTheRoundBeingSettled()
    {
        // The same four people, dealt in a different order: Alice now sits second, so the seat
        // above her is Bob and not Dan. Nothing about the streak changed — only where everybody
        // is sitting for the round that settles it.
        PlayerId[] seating = [Bob, Alice, Carol, Dan];

        var order = Order(DealBuilder.ForPlayers(4).Give(1, WinningHand));
        var agents = FourPlayers.ToDictionary(
            player => player,
            player => player == Alice
                ? (IPlayerAgent)new ScriptedPlayerAgent(new ScriptedTurn { Declare = true })
                : ScriptedPlayerAgent.Passive());

        var result = new RoundEngine(
            seating, agents, Stakes.Standard, order, new Random(1),
            streak: new WinStreak(Alice, 2)).Play();

        Assert.Equal(Alice, result.Winner);
        Assert.True(result.Win.ThirdConsecutiveWin);

        // $30 from Bob, nothing from Carol or Dan.
        var rounds = Settlement.RoundPayments(seating, Alice, Stakes.Standard, result.Win);

        Assert.Equal(-30, rounds[Bob]);
        Assert.Equal(0, rounds[Carol]);
        Assert.Equal(0, rounds[Dan]);
        Assert.Equal(30, rounds[Alice]);
    }

    [Fact]
    public void ARoundInNoStreakBillsEverybodyAsItAlwaysDid()
    {
        var order = Order(DealBuilder.ForPlayers(4).Give(0, WinningHand));
        var agents = Agents(new ScriptedPlayerAgent(new ScriptedTurn { Declare = true }));

        // Somebody else's two wins are not this winner's streak.
        var result = new RoundEngine(
            FourPlayers, agents, Stakes.Standard, order, new Random(1),
            streak: new WinStreak(Bob, 2)).Play();

        Assert.False(result.Win.ThirdConsecutiveWin);
        Assert.Equal(
            new Dictionary<PlayerId, int> { [Alice] = 28, [Bob] = -8, [Carol] = -12, [Dan] = -8 },
            result.Payouts);
    }

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

        public bool ObjectToClaim(TurnContext context) => false;

        public bool Declare(TurnContext context) => false;
    }
}
