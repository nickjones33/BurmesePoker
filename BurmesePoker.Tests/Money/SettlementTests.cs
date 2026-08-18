using System.Reflection;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Money;

/// <summary>
/// Settling a finished round (RULES.md §4.3, §4.4, §7.2): the flat round payment to the
/// winner, plus the pairwise money-card side-bet paid to each card's <b>owner</b>.
/// </summary>
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
        Settlement.ForRound(FivePlayers, Ava, Stakes.Standard, moneyCards, ownership, Shoe);

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
    public void APlayerOwningOneDoubleMoneyCardCollectsEightFromTheTable()
    {
        // The §4.3 worked example, line 3: turning up the 7♦ doubles a permanent money card,
        // so one card pays 2 × $1 × 4 = $8 — the same as owning two singles.
        var deltas = Settle(
            TurnedUp(Copy(Rank.Seven, Suit.Diamonds)),
            OwnedBy(Bo, Copy(Rank.Seven, Suit.Diamonds)));

        Assert.Equal(-5 + 8, deltas[Bo]);
        Assert.Equal(20 - 2, deltas[Ava]);
        Assert.Equal([-7, -7, -7], new[] { Cy, Di, Eve }.Select(player => deltas[player]));
        Assert.Equal(0, deltas.Values.Sum());
    }

    [Fact]
    public void ADoubleMoneyCardPaysTwiceTheMoneyCardValuePerOpponent()
    {
        var single = Settle(
            TurnedUp(Copy(Rank.Five, Suit.Hearts)),
            OwnedBy(Bo, Copy(Rank.Five, Suit.Hearts)));

        var doubled = Settle(
            TurnedUp(Copy(Rank.Ace, Suit.Spades)),
            OwnedBy(Bo, Copy(Rank.Ace, Suit.Spades)));

        Assert.Equal(-5 + 4, single[Bo]);
        Assert.Equal(-5 + 8, doubled[Bo]);
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
            TurnedUp(Copy(Rank.Five, Suit.Hearts)), ownership, Shoe);

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
            TurnedUp(), OwnedBy(Bo, Copy(Rank.Ace, Suit.Spades)), Shoe);

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
                new MoneyCardRegistry(turnedUp), ownership, Shoe);

            Assert.Equal(0, deltas.Values.Sum());
        }
    }

    [Fact]
    public void SettlementIsNeverGivenAHand()
    {
        // RULES.md §4.4: the question at settlement is never "who holds this card?". The
        // parameter list is where that rule is enforced — nothing here can reach a hand.
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
                typeof(IReadOnlyList<Card>)
            ],
            parameters);
    }

    [Fact]
    public void AWinnerWhoIsNotAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo], Cy, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe));

    [Fact]
    public void TheSamePlayerTwiceAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo, Ava], Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe));

    [Fact]
    public void AnEmptyTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [], Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), Shoe));

    [Fact]
    public void ACardOwnedBySomebodyNotAtTheTableIsRejected() =>
        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            [Ava, Bo], Ava, Stakes.Standard, TurnedUp(),
            OwnedBy(Eve, Copy(Rank.Ace, Suit.Spades)), Shoe));

    [Fact]
    public void AShuffledDeckIsRejectedAsTheShoe()
    {
        // Ownership is by CardId and designation is by value, so settlement resolves ids
        // through the shoe by index. Deck.Cards is shuffled — passing it would silently
        // settle the wrong cards.
        var deck = Deck.TwoDecks();
        deck.Shuffle(new Random(1));

        var exception = Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), new CardOwnership(), deck.Cards));

        Assert.Contains("BuildTwoDecks", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOwnedCardOutsideTheShoeIsRejected()
    {
        var ownership = new CardOwnership();
        ownership.RecordFromDeck(new CardId(500), Ava);

        Assert.Throws<ArgumentException>(() => Settlement.ForRound(
            FivePlayers, Ava, Stakes.Standard, TurnedUp(), ownership, Shoe));
    }
}
