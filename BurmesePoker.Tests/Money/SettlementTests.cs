using System.Reflection;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Money;

/// <summary>
/// Settling a finished round (RULES.md §4.3, §4.4, §7.2, §7.3): the round payment to the
/// winner, plus the pairwise money-card side-bet paid to each card's <b>owner</b>.
/// </summary>
/// <remarks>
/// ⚠️ <b>Every test below that works a payout out by hand declares <see cref="Jokered"/></b>,
/// which is the flat case §7.2 has named since rev 1. The §7.3 bonus has its own tests at the
/// foot of the file, and the flat ones say <i>jokered</i> rather than saying nothing so that
/// the multiplier is visible at every site it could have applied to.
/// </remarks>
public class SettlementTests
{
    private static readonly IReadOnlyList<Card> Shoe = DeckBuilder.BuildTwoDecks();

    private static readonly PlayerId Ava = new(0);
    private static readonly PlayerId Bo = new(1);
    private static readonly PlayerId Cy = new(2);
    private static readonly PlayerId Di = new(3);
    private static readonly PlayerId Eve = new(4);

    private static readonly IReadOnlyList<PlayerId> FivePlayers = [Ava, Bo, Cy, Di, Eve];

    /// <summary>One of the two physical cards of this value in the shoe.</summary>
    private static Card Copy(Rank rank, Suit suit, int copy = 0) =>
        Shoe.Where(card => card.Rank == rank && card.Suit == suit).ElementAt(copy);

    /// <summary>Five players at the standard stakes, with Ava going out.</summary>
    private static IReadOnlyDictionary<PlayerId, int> Settle(
        MoneyCardRegistry moneyCards, CardOwnership ownership) =>
        Settlement.ForRound(FivePlayers, Ava, Stakes.Standard, moneyCards, ownership, Shoe, Win.Declared(Jokered()));

    /// <summary>
    /// A declared thirteen with a joker in it — no §7.3 bonus, so the round pays flat.
    /// </summary>
    private static IReadOnlyList<Card> Jokered() =>
        [.. Shoe.Where(card => !card.IsJoker).Take(12), Shoe.First(card => card.IsJoker)];

    /// <summary>A declared thirteen with no joker anywhere — the §7.3 bonus qualifies.</summary>
    private static IReadOnlyList<Card> Jokerless() => [.. Shoe.Where(card => !card.IsJoker).Take(13)];

    private static MoneyCardRegistry TurnedUp(params Card[] cards) => new(cards);

    private static CardOwnership OwnedBy(PlayerId owner, params Card[] cards)
    {
        var ownership = new CardOwnership();

        foreach (var card in cards)
        {
            ownership.RecordFromDeck(card.Id, owner);
        }

        return ownership;
    }

    [Fact]
    public void WithNoOwnedMoneyCardsEveryLoserPaysTheWinnerTheRoundValue()
    {
        // RULES.md §7.2 step 1, and the §4.3 worked example: 4 × $5 = $20.
        var deltas = Settle(TurnedUp(Copy(Rank.Five, Suit.Hearts)), new CardOwnership());

        Assert.Equal(20, deltas[Ava]);
        Assert.Equal([-5, -5, -5, -5], new[] { Bo, Cy, Di, Eve }.Select(player => deltas[player]));
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void APlayerOwningTwoMoneyCardsCollectsEightFromTheTable()
    {
        // The §4.3 worked example, line 2: 2 cards × $1 × 4 opponents = $8, on top of the
        // $5 Bo pays for losing the round.
        var deltas = Settle(
            TurnedUp(Copy(Rank.Five, Suit.Hearts)),
            OwnedBy(Bo, Copy(Rank.Five, Suit.Hearts, 0), Copy(Rank.Five, Suit.Hearts, 1)));

        Assert.Equal(-5 + 8, deltas[Bo]);
        Assert.Equal(20 - 2, deltas[Ava]);
        Assert.Equal([-7, -7, -7], new[] { Cy, Di, Eve }.Select(player => deltas[player]));
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void APlayerOwningOneTripledMoneyCardCollectsTwelveFromTheTable()
    {
        // The §4.3 worked example, line 3: turning up the 7♦ triples the value, so its
        // partner pays 3 × $1 × 4 = $12 — half again as much as owning two singles.
        var deltas = Settle(
            TurnedUp(Copy(Rank.Seven, Suit.Diamonds)),
            OwnedBy(Bo, Copy(Rank.Seven, Suit.Diamonds, 1)));

        Assert.Equal(-5 + 12, deltas[Bo]);
        Assert.Equal(20 - 3, deltas[Ava]);
        Assert.Equal([-8, -8, -8], new[] { Cy, Di, Eve }.Select(player => deltas[player]));
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void ATripledMoneyCardPaysThreeTimesTheMoneyCardValuePerOpponent()
    {
        var single = Settle(
            TurnedUp(Copy(Rank.Five, Suit.Hearts)),
            OwnedBy(Bo, Copy(Rank.Five, Suit.Hearts)));

        var tripled = Settle(
            TurnedUp(Copy(Rank.Ace, Suit.Spades)),
            OwnedBy(Bo, Copy(Rank.Ace, Suit.Spades, 1)));

        Assert.Equal(-5 + 4, single[Bo]);
        Assert.Equal(-5 + 12, tripled[Bo]);
    }

    [Fact]
    public void OwningBothPartnersOfASevenAndAceTurnUpCollectsFortyFromTheTable()
    {
        // 🔥 The §4.3 worked example, line 4, and the largest single swing in the game:
        // (5 + 5) × $1 × 4 = $40 a head at standard stakes, against a $5 round prize
        // (RULES.md §4.1, rev 21).
        var deltas = Settle(
            TurnedUp(Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)),
            OwnedBy(Bo, Copy(Rank.Seven, Suit.Diamonds, 1), Copy(Rank.Ace, Suit.Spades, 1)));

        Assert.Equal(-5 + 40, deltas[Bo]);
        Assert.Equal(20 - 10, deltas[Ava]);
        Assert.Equal([-15, -15, -15], new[] { Cy, Di, Eve }.Select(player => deltas[player]));
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void TheSameTurnUpWithThePartnersSplitSettlesAtTwentyFour()
    {
        // The same round, the same two cards, and the only thing that moved is who the deck
        // gave them to: two players collect 3 × $1 × 4 = $12 each, so the pair moves $24
        // instead of $40. This is the pair of tests the ×5 exists for.
        var ownership = new CardOwnership();
        ownership.RecordFromDeck(Copy(Rank.Seven, Suit.Diamonds, 1).Id, Bo);
        ownership.RecordFromDeck(Copy(Rank.Ace, Suit.Spades, 1).Id, Cy);

        var deltas = Settle(
            TurnedUp(Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)),
            ownership);

        Assert.Equal(-5 + 12 - 3, deltas[Bo]);
        Assert.Equal(-5 + 12 - 3, deltas[Cy]);
        Assert.Equal(20 - 6, deltas[Ava]);
        Assert.Equal([-11, -11], new[] { Di, Eve }.Select(player => deltas[player]));
        Assert.Equal(0, deltas.Values.Sum());

        // The pair collects $24 here and $40 in the test above, from the same designation and
        // the same two physical cards: a player who owns neither pays $6 of side-bet against
        // $10, and that difference is the whole of the jackpot.
        var jackpot = Settle(
            TurnedUp(Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)),
            OwnedBy(Bo, Copy(Rank.Seven, Suit.Diamonds, 1), Copy(Rank.Ace, Suit.Spades, 1)));

        Assert.Equal(-6, deltas[Eve] + 5);
        Assert.Equal(-10, jackpot[Eve] + 5);
    }

    [Fact]
    public void EveryJokerPaysItsOwnerInEveryRound()
    {
        // RULES.md §4.1, rev 21: jokers are permanent money cards, so a joker nobody turned
        // up still pays 1 × $1 × 4.
        var joker = Shoe.First(card => card.IsJoker);
        var deltas = Settle(TurnedUp(Copy(Rank.Five, Suit.Hearts)), OwnedBy(Bo, joker));

        Assert.Equal(-5 + 4, deltas[Bo]);
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void ThePermanentMoneyCardsPayWithNothingTurnedUp()
    {
        // 7♦ and A♠ pay in every round (RULES.md §4.1), with no turned-up card involved.
        var deltas = Settle(TurnedUp(), OwnedBy(Bo, Copy(Rank.Ace, Suit.Spades)));

        Assert.Equal(-5 + 4, deltas[Bo]);
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void AnOwnedCardThatIsNotAMoneyCardPaysNothing()
    {
        var deltas = Settle(
            TurnedUp(Copy(Rank.Five, Suit.Hearts)),
            OwnedBy(Bo, Copy(Rank.Nine, Suit.Clubs), Copy(Rank.King, Suit.Hearts)));

        Assert.Equal(-5, deltas[Bo]);
        Assert.Equal(20, deltas[Ava]);
    }

    [Fact]
    public void AMoneyCardHeldByAnotherPlayerStillPaysTheOwner()
    {
        // The deck dealt this 5♥ to Bo; Cy picked it out of a discard pile and is holding it
        // at the end of the round. Ownership never transfers (RULES.md §4.4), so Bo collects
        // and Cy pays — and settlement is not even told who is holding what.
        var deltas = Settle(
            TurnedUp(Copy(Rank.Five, Suit.Hearts)),
            OwnedBy(Bo, Copy(Rank.Five, Suit.Hearts)));

        Assert.Equal(-5 + 4, deltas[Bo]);
        Assert.Equal(-5 - 1, deltas[Cy]);
    }

    [Fact]
    public void AMoneyCardItsOwnerDiscardedStillPaysThatOwner()
    {
        // The headline case for permanent ownership (RULES.md §4.4 rule 2). Bo drew the A♠
        // and threw it away; it sat in the discard pile for the rest of the round. If
        // settlement read hands rather than ownership records, this would pay nobody.
        var deltas = Settle(TurnedUp(), OwnedBy(Bo, Copy(Rank.Ace, Suit.Spades)));

        Assert.Equal(-5 + 4, deltas[Bo]);
        Assert.Equal([-6, -6, -6], new[] { Cy, Di, Eve }.Select(player => deltas[player]));
    }

    [Fact]
    public void AMoneyCardNobodyDrewIsOwnedByNobodyAndPaysNobody()
    {
        // Both 5♥ are still in the deck, or turned up on the table, at the end of the round.
        var deltas = Settle(TurnedUp(Copy(Rank.Five, Suit.Hearts)), new CardOwnership());

        Assert.Equal(20, deltas[Ava]);
        Assert.Equal([-5, -5, -5, -5], new[] { Bo, Cy, Di, Eve }.Select(player => deltas[player]));
    }

    [Fact]
    public void TheWinnerTakesPartInTheMoneySettlementToo()
    {
        // Money-card settlement resolves independently of who won (RULES.md §4.3).
        var deltas = Settle(TurnedUp(), OwnedBy(Ava, Copy(Rank.Seven, Suit.Diamonds)));

        Assert.Equal(20 + 4, deltas[Ava]);
        Assert.Equal([-6, -6, -6, -6], new[] { Bo, Cy, Di, Eve }.Select(player => deltas[player]));
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void MoneyOwedInBothDirectionsNets()
    {
        // Everyone owes everyone else (RULES.md §4.3): Bo and Cy each own one money card, so
        // the dollar between them cancels and only the third player's payments show.
        var ownership = new CardOwnership();
        ownership.RecordFromDeck(Copy(Rank.Five, Suit.Hearts, 0).Id, Bo);
        ownership.RecordFromDeck(Copy(Rank.Five, Suit.Hearts, 1).Id, Cy);

        var deltas = Settlement.ForRound(
            [Ava, Bo, Cy], Ava, Stakes.Standard,
            TurnedUp(Copy(Rank.Five, Suit.Hearts)), ownership, Shoe, Win.Declared(Jokered()));

        Assert.Equal(10 - 2, deltas[Ava]);
        Assert.Equal(-5 + 2 - 1, deltas[Bo]);
        Assert.Equal(-5 + 2 - 1, deltas[Cy]);
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void EveryPlayerAppearsEvenWhenTheirDeltaIsZero()
    {
        // Heads-up at $5/$5: Bo's money card exactly cancels the round he lost.
        var deltas = Settlement.ForRound(
            [Ava, Bo], Ava, new Stakes(5, 5),
            TurnedUp(), OwnedBy(Bo, Copy(Rank.Ace, Suit.Spades)), Shoe, Win.Declared(Jokered()));

        Assert.Equal([Ava, Bo], deltas.Keys.OrderBy(player => player.Value));
        Assert.Equal(0, deltas[Ava]);
        Assert.Equal(0, deltas[Bo]);
    }

    [Fact]
    public void DeltasAlwaysSumToZeroForAnyConfiguration()
    {
        var random = new Random(20260818);

        for (var round = 0; round < 500; round++)
        {
            var players = Enumerable.Range(0, random.Next(2, 7)).Select(seat => new PlayerId(seat)).ToList();
            var stakes = new Stakes(random.Next(1, 20), random.Next(1, 20));
            var turnedUp = Enumerable.Range(0, random.Next(0, 3))
                .Select(_ => Shoe[random.Next(Shoe.Count)])
                .ToArray();

            // Distinct cards: one physical card cannot come from the deck twice.
            var deal = Shoe.ToArray();
            random.Shuffle(deal);

            var ownership = new CardOwnership();

            foreach (var card in deal.Take(random.Next(0, 80)))
            {
                ownership.RecordFromDeck(card.Id, players[random.Next(players.Count)]);
            }

            var deltas = Settlement.ForRound(
                players, players[random.Next(players.Count)], stakes,
                new MoneyCardRegistry(turnedUp), ownership, Shoe,
                Win.Declared(random.Next(2) == 0 ? Jokerless() : Jokered()));

            Assert.Equal(0, deltas.Values.Sum());
        }
    }

    [Fact]
    public void TheOnlyHandSettlementIsGivenIsTheWinnersDeclaredThirteen()
    {
        // RULES.md §4.4: the question the *side bet* asks is never "who holds this card?", and
        // the parameter list is where that is enforced — no table, no seat, no player state.
        // ⚠️ This test used to be called SettlementIsNeverGivenAHand and asserted six
        // parameters. §7.3 gave it a seventh, and P35 turned that seventh from the declared
        // thirteen into a Win — which is a stronger form of the same guarantee: what arrives is
        // three booleans about how the round ended, so step 2 cannot ask about a hand at all.
        // ⚠️ And no match, no history: §7.5's streak is told, never counted here.
        var parameters = typeof(Settlement)
            .GetMethod(nameof(Settlement.ForRound), BindingFlags.Public | BindingFlags.Static)!
            .GetParameters()
            .Select(parameter => parameter.ParameterType);

        Assert.Equal(
            [
                typeof(IReadOnlyList<PlayerId>),
                typeof(PlayerId),
                typeof(Stakes),
                typeof(MoneyCardRegistry),
                typeof(CardOwnership),
                typeof(IReadOnlyList<Card>),
                typeof(Win)
            ],
            parameters);
    }

    [Fact]
    public void AWinnerWhoIsNotAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo], Cy, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Win.Declared(Jokered())));

    [Fact]
    public void TheSamePlayerTwiceAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo, Ava], Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Win.Declared(Jokered())));

    [Fact]
    public void AnEmptyTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [], Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Win.Declared(Jokered())));

    [Fact]
    public void ACardOwnedBySomebodyNotAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo], Ava, Stakes.Standard, TurnedUp(),
            OwnedBy(Eve, Copy(Rank.Ace, Suit.Spades)), Shoe, Win.Declared(Jokered())));

    [Fact]
    public void AShuffledDeckIsRejectedAsTheShoe()
    {
        // Ownership is by CardId and designation is by value, so settlement resolves ids
        // through the shoe by index. Deck.Cards is shuffled — passing it would silently
        // settle the wrong cards.
        var deck = Deck.TwoDecks();
        deck.Shuffle(new Random(1));

        var exception = Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), deck.Cards,
            Win.Declared(Jokered())));

        Assert.Contains("BuildTwoDecks", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOwnedCardOutsideTheShoeIsRejected()
    {
        var ownership = new CardOwnership();
        ownership.RecordFromDeck(new CardId(500), Ava);

        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe, Win.Declared(Jokered())));
    }

    // ── RULES.md §7.3: the clean bonus ───────────────────────────────────────────────────
    //
    // A *jokerless* declaration multiplies the round payment: ×2 at two, three or four seats
    // and ×3 at five or more. The condition is the whole declared thirteen, so a joker in a
    // set forfeits it exactly as one in a run does — which is the correction rev 26 made to
    // rev 25's "all series clean".

    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    [InlineData(6, 3)]
    public void AJokerlessDeclarationMultipliesTheRoundPaymentByTheTableSize(int seats, int multiplier)
    {
        var players = Enumerable.Range(0, seats).Select(seat => new PlayerId(seat)).ToArray();
        var winner = players[0];

        var flat = Settlement.ForRound(
            players, winner, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Win.Declared(Jokered()));
        var bonus = Settlement.ForRound(
            players, winner, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Win.Declared(Jokerless()));

        // The losers each pay the round value, times the multiplier, and nothing else moves.
        Assert.Equal(5 * (seats - 1), flat[winner]);
        Assert.Equal(5 * multiplier * (seats - 1), bonus[winner]);
        Assert.All(players.Skip(1), player => Assert.Equal(-5 * multiplier, bonus[player]));
        Assert.Equal(0, bonus.Values.Sum());
    }

    [Fact]
    public void AJokerInASetForfeitsTheBonusExactlyAsOneInARunDoes()
    {
        // 🔥 The discriminating case, and the whole of what rev 26 corrected. These thirteen
        // are four clean series and one set — 2♥–5♥, 7♦–9♦, 4♣–6♣ and three queens, one of
        // them a joker. Every *series* here is clean, so rev 25's withdrawn reading would have
        // paid the bonus; rev 26 asks the thirteen and does not.
        var jokerInASet = Hands.Of(
            "2H", "3H", "4H", "5H", "7D", "8D", "9D", "4C", "5C", "6C", "QS", "QD", "RJ");

        Assert.False(Settlement.IsJokerless(jokerInASet));

        var deltas = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe,
            Win.Declared(jokerInASet));

        Assert.Equal(4 * 5, deltas[Ava]);
    }

    [Fact]
    public void FiveHandedFourSetsAndNoSeriesAtAllStillPaysTripleIfItIsJokerless()
    {
        // §9 #35's answer, and the thing a hand-wide predicate can express and a series-wide
        // one cannot: at five seats §7.1.1 requires no series, so this is a legal declaration
        // holding none — and it is jokerless, so it pays ×3.
        var fourSets = Hands.Of(
            "2H", "2S", "2C", "5D", "5H", "5S", "9C", "9D", "9H", "KS", "KC", "KD", "KH");

        Assert.True(Settlement.IsJokerless(fourSets));

        var deltas = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe,
            Win.Declared(fourSets));

        Assert.Equal(4 * 15, deltas[Ava]);
        Assert.All(new[] { Bo, Cy, Di, Eve }, player => Assert.Equal(-15, deltas[player]));
    }

    [Fact]
    public void TheBonusDoesNotReachTheMoneyCardSettlement()
    {
        // RULES.md §9 #36's recorded default, fenced: three sayings name "the winning prize"
        // and none names the money, so step 2 is untouched. ⚠️ If the expert answers otherwise
        // this test is the one that has to change, which is what it is for.
        var ownership = OwnedBy(Bo, Copy(Rank.Ace, Suit.Spades));

        var flat = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe, Win.Declared(Jokered()));
        var bonus = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe, Win.Declared(Jokerless()));

        // The A♠ is a permanent money card and nothing designates it, so it pays ×1 — $1 a
        // head from four opponents. Bo's side-bet take is $4 either way; only the round
        // payment moved, from $5 to $15.
        Assert.Equal(-5 + 4, flat[Bo]);
        Assert.Equal(-15 + 4, bonus[Bo]);
        Assert.Equal(flat[Bo] + 5, bonus[Bo] + 15);
    }

    [Theory]
    [InlineData(2, false, 5)]
    [InlineData(4, true, 10)]
    [InlineData(5, true, 15)]
    [InlineData(6, true, 15)]
    public void WhatOneLoserPaysIsPublishedSoAConsumerSplitsTheNetWhereTheDomainDid(
        int seats, bool jokerless, int expected) =>
        Assert.Equal(
            expected,
            Settlement.RoundPayment(
                Stakes.Standard, TableRules.For(seats), new Win(jokerless, false, false)));

    // ---------------------------------------------------------------------------------------
    // RULES.md §7.4 — the deal bonus, and §7.5 — the feeding blame. Packet P35.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void AWinFromTheInitialDealPaysDouble()
    {
        // §7.4: "if you win on an initial deal you get double payout." The thirteen here hold a
        // joker, so this is the deal bonus on its own with no §7.3 bonus underneath it.
        var flat = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe,
            Win.Declared(Jokered()));

        var onTheDeal = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe,
            Win.Declared(Jokered(), fromTheInitialDeal: true));

        Assert.Equal(4 * 5, flat[Ava]);
        Assert.Equal(4 * 10, onTheDeal[Ava]);
        Assert.All(new[] { Bo, Cy, Di, Eve }, player => Assert.Equal(-10, onTheDeal[player]));
        Assert.Equal(0, onTheDeal.Values.Sum());
    }

    /// <summary>
    /// ⚠️ <b>RULES.md §9 #39's recorded default, fenced.</b> Nothing in either saying mentions
    /// the other, so the two multipliers <b>multiply</b> — a jokerless win from the deal at five
    /// seats pays ×3 × ×2 = ×6. The competing reading is that <i>"double payout"</i> doubles the
    /// round <b>value</b> rather than the payout, which differs only once another multiplier
    /// exists. <b>The day an expert answers, this is the test that fails.</b>
    /// </summary>
    [Fact]
    public void TheTwoBonusesMultiplyUntilTheExpertSaysOtherwise()
    {
        var both = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe,
            Win.Declared(Jokerless(), fromTheInitialDeal: true));

        // $5 × 3 (jokerless, five-handed) × 2 (on the deal) = $30 a head, from four of them.
        Assert.Equal(4 * 30, both[Ava]);
        Assert.All(new[] { Bo, Cy, Di, Eve }, player => Assert.Equal(-30, both[player]));

        Assert.Equal(
            30, Settlement.RoundPayment(Stakes.Standard, TableRules.For(5), new Win(true, true, false)));
        Assert.Equal(
            20, Settlement.RoundPayment(Stakes.Standard, TableRules.For(4), new Win(true, true, false)));
    }

    /// <summary>
    /// ⚠️ <b>RULES.md §9 #40's recorded default, fenced</b> — the deal bonus is a multiplier on
    /// the round payment and reaches the money-card settlement no more than §7.3's does (#36).
    /// </summary>
    [Fact]
    public void TheDealBonusDoesNotReachTheMoneyCards()
    {
        var ownership = OwnedBy(Bo, Copy(Rank.Seven, Suit.Diamonds));

        var flat = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe, Win.Declared(Jokered()));

        var onTheDeal = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe,
            Win.Declared(Jokered(), fromTheInitialDeal: true));

        // Bo's 7♦ pays him $1 a head from four opponents either way; only the round moved.
        Assert.Equal(-5 + 4, flat[Bo]);
        Assert.Equal(-10 + 4, onTheDeal[Bo]);
    }

    [Fact]
    public void AThirdConsecutiveWinIsPaidEntirelyByTheSeatAboveTheWinner()
    {
        // §7.5: "if you win three in a row then the player proceeding you in turn order pays
        // your whole payout (blamed for feeding)." Ava is seat 0, so the seat above her is Eve
        // — the last in turn order, who discards into her.
        var streak = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe,
            Win.Declared(Jokered(), thirdConsecutiveWin: true));

        Assert.Equal(4 * 5, streak[Ava]);
        Assert.Equal(-20, streak[Eve]);
        Assert.All(new[] { Bo, Cy, Di }, player => Assert.Equal(0, streak[player]));
        Assert.Equal(0, streak.Values.Sum());
    }

    [Fact]
    public void TheStreakSubstitutionIsMadeAfterTheMultipliersAndNotBefore()
    {
        // A jokerless third consecutive win at five seats: one seat pays ×3 the round value
        // four times over, which is the whole of what four ordinary losers would have paid.
        var streak = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe,
            Win.Declared(Jokerless(), thirdConsecutiveWin: true));

        Assert.Equal(4 * 15, streak[Ava]);
        Assert.Equal(-60, streak[Eve]);
    }

    /// <summary>
    /// ⚠️ <b>RULES.md §9 #44's recorded default, fenced</b> — the substitution is about the round
    /// payment, and §7.2 step 2 stays pairwise and mutual with the winner one participant in it
    /// like anybody else.
    /// </summary>
    [Fact]
    public void TheStreakSubstitutionDoesNotReachTheMoneyCards()
    {
        var ownership = OwnedBy(Cy, Copy(Rank.Ace, Suit.Spades));

        var streak = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe,
            Win.Declared(Jokered(), thirdConsecutiveWin: true));

        // Cy pays nothing towards the round — Eve is carrying all of it — and still collects
        // $1 a head from the other four for the ace of spades.
        Assert.Equal(4, streak[Cy]);
        Assert.Equal(-20 - 1, streak[Eve]);
    }

    [Fact]
    public void TheSeatAboveIsTheOneThatDiscardsIntoYouAndItWrapsRoundTheTable()
    {
        Assert.Equal(Eve, Settlement.SeatAbove(FivePlayers, Ava));
        Assert.Equal(Ava, Settlement.SeatAbove(FivePlayers, Bo));
        Assert.Equal(Di, Settlement.SeatAbove(FivePlayers, Eve));

        // The order is the whole of the answer: the same five people, dealt differently, name a
        // different payer — which is why §7.5 is settled from the round's own seating (§9 #46).
        Assert.Equal(Bo, Settlement.SeatAbove([Bo, Ava, Cy, Di, Eve], Ava));

        Assert.Throws<ArgumentException>(() => Settlement.SeatAbove(FivePlayers, new PlayerId(9)));
    }

    [Fact]
    public void TheRoundColumnIsWhatTheDomainActuallySettledInEveryCase()
    {
        // BUILD-PLAN P35 build item 4: a consumer splitting a net delta must split it where the
        // domain did. This is that guarantee, over every shape a win can now have.
        var ownership = OwnedBy(Di, Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades));

        foreach (var win in new[]
                 {
                     Win.Ordinary,
                     new Win(true, false, false),
                     new Win(false, true, false),
                     new Win(false, false, true),
                     new Win(true, true, true)
                 })
        {
            var settled = Settlement.ForRound(
                FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe, win);
            var rounds = Settlement.RoundPayments(FivePlayers, Ava, Stakes.Standard, win);

            Assert.Equal(0, rounds.Values.Sum());

            // Whatever is left when the round column is taken out is the side bet, and the side
            // bet is the same under every win: $1 a head on two cards, to Di.
            var sideBet = FivePlayers.ToDictionary(
                player => player, player => settled[player] - rounds[player]);

            Assert.Equal(8, sideBet[Di]);
            Assert.All(new[] { Ava, Bo, Cy, Eve }, player => Assert.Equal(-2, sideBet[player]));
        }
    }

    [Fact]
    public void TheJokerlessPredicateIsAboutCardsAndNotAboutMelds()
    {
        // A joker held is a joker declared: the predicate cannot be dodged by covering it
        // inside a meld, because it never looks at a meld.
        Assert.True(Settlement.IsJokerless(Hands.Of("2H", "3H", "4H")));
        Assert.False(Settlement.IsJokerless(Hands.Of("2H", "3H", "RJ")));
        Assert.False(Settlement.IsJokerless(Hands.Of("BJ", "RJ", "BJ")));
        Assert.True(Settlement.IsJokerless([]));
    }
}
