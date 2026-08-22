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
        Settlement.ForRound(FivePlayers, Ava, Stakes.Standard, moneyCards, ownership, Shoe, Jokered());

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
            TurnedUp(Copy(Rank.Five, Suit.Hearts)), ownership, Shoe, Jokered());

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
            TurnedUp(), OwnedBy(Bo, Copy(Rank.Ace, Suit.Spades)), Shoe, Jokered());

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
                random.Next(2) == 0 ? Jokerless() : Jokered());

            Assert.Equal(0, deltas.Values.Sum());
        }
    }

    [Fact]
    public void TheOnlyHandSettlementIsGivenIsTheWinnersDeclaredThirteen()
    {
        // RULES.md §4.4: the question the *side bet* asks is never "who holds this card?", and
        // the parameter list is where that is enforced — no table, no seat, no player state.
        // ⚠️ This test used to be called SettlementIsNeverGivenAHand and asserted six
        // parameters. §7.3 gave it a seventh, and the rule it guards did not change: what
        // arrives is a list of cards with no owner attached to it, so step 2 still cannot ask.
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
                typeof(IReadOnlyList<Card>)
            ],
            parameters);
    }

    [Fact]
    public void AWinnerWhoIsNotAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo], Cy, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Jokered()));

    [Fact]
    public void TheSamePlayerTwiceAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo, Ava], Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Jokered()));

    [Fact]
    public void AnEmptyTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [], Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Jokered()));

    [Fact]
    public void ACardOwnedBySomebodyNotAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo], Ava, Stakes.Standard, TurnedUp(),
            OwnedBy(Eve, Copy(Rank.Ace, Suit.Spades)), Shoe, Jokered()));

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
            Jokered()));

        Assert.Contains("BuildTwoDecks", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOwnedCardOutsideTheShoeIsRejected()
    {
        var ownership = new CardOwnership();
        ownership.RecordFromDeck(new CardId(500), Ava);

        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe, Jokered()));
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
            players, winner, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Jokered());
        var bonus = Settlement.ForRound(
            players, winner, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, Jokerless());

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
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, jokerInASet);

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
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe, fourSets);

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
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe, Jokered());
        var bonus = Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe, Jokerless());

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
            Settlement.RoundPayment(Stakes.Standard, TableRules.For(seats), jokerless));

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
