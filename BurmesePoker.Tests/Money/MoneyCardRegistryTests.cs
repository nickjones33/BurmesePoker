using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Money;

/// <summary>
/// Designation (RULES.md §4.1, §4.2). Every case here is about <i>value</i>, never instance —
/// a designator names a card value, and both copies of that value across the two decks are
/// money cards.
/// </summary>
public class MoneyCardRegistryTests
{
    private static Card Card_(string code) => Hands.Of(code)[0];

    private static MoneyCardRegistry Registry(params string[] turnedUp) =>
        new(Hands.Of(turnedUp));

    [Fact]
    public void SevenOfDiamondsAceOfSpadesAndEveryJokerAreMoneyCardsWithNothingTurnedUp()
    {
        // RULES.md §4.1, rev 21: three permanent *values*, not two. The jokers were added
        // when somebody answered past the question they were asked.
        var registry = Registry();

        Assert.Equal(1, registry.Multiplier(Card_("7D")));
        Assert.Equal(1, registry.Multiplier(Card_("AS")));
        Assert.Equal(1, registry.Multiplier(Card_("RJ")));
        Assert.Equal(1, registry.Multiplier(Card_("BJ")));
    }

    [Fact]
    public void ThePermanentSideBetIsEightCardsBeforeAnythingIsTurnedUp()
    {
        // 🔥 The count is the figure everything derived from the size of the side-bet moves
        // with (RULES.md §4.3, §4.4): two 7♦, two A♠ and **four** jokers.
        var registry = Registry();
        var shoe = Deck.TwoDecks().Cards;

        Assert.Equal(8, shoe.Count(card => registry.Multiplier(card) > 0));
        Assert.Equal(4, shoe.Count(card => card.IsJoker && registry.Multiplier(card) == 1));
    }

    [Fact]
    public void BothCopiesOfAPermanentMoneyCardCount()
    {
        var registry = Registry();
        var shoe = Deck.TwoDecks().Cards;

        var sevens = shoe.Where(c => c.Rank == Rank.Seven && c.Suit == Suit.Diamonds).ToList();

        Assert.Equal(2, sevens.Count);
        Assert.All(sevens, card => Assert.Equal(1, registry.Multiplier(card)));
    }

    [Theory]
    [InlineData("7H")]
    [InlineData("7S")]
    [InlineData("7C")]
    [InlineData("AD")]
    [InlineData("AH")]
    [InlineData("AC")]
    [InlineData("3C")]
    public void NothingElseIsPermanent(string code) =>
        Assert.Equal(0, Registry().Multiplier(Card_(code)));

    [Fact]
    public void BothCopiesOfEveryJokerColourCount()
    {
        var registry = Registry();
        var shoe = Deck.TwoDecks().Cards;

        Assert.Equal(2, shoe.Count(card => card.IsJoker && card.Color == CardColor.Red));
        Assert.All(shoe.Where(card => card.IsJoker), card => Assert.Equal(1, registry.Multiplier(card)));
    }

    [Fact]
    public void ATurnedUpFiveOfHeartsDesignatesOnlyTheFiveOfHearts()
    {
        // RULES.md §4.2: exact rank *and* suit. The rejected Indian Rummy reading would
        // designate all eight fives.
        var registry = Registry("5H");

        Assert.Equal(1, registry.Multiplier(Card_("5H")));
        Assert.Equal(0, registry.Multiplier(Card_("5D")));
        Assert.Equal(0, registry.Multiplier(Card_("5S")));
        Assert.Equal(0, registry.Multiplier(Card_("5C")));
    }

    [Fact]
    public void ATurnedUpFiveOfHeartsDesignatesBothCopiesInTheShoe()
    {
        var registry = Registry("5H");
        var shoe = Deck.TwoDecks().Cards;

        var designated = shoe.Where(c => registry.Multiplier(c) > 0).ToList();

        // Both 5♥ on top of the eight permanent cards — ten, and nothing else.
        Assert.Equal(10, designated.Count);
        Assert.Equal(2, designated.Count(c => c.Rank == Rank.Five && c.Suit == Suit.Hearts));
    }

    [Fact]
    public void ATurnedUpSevenOfDiamondsTriplesIt()
    {
        // RULES.md §4.1, rev 20: a designation landing on a permanent money card pays ×3,
        // which is what conserves what a designation is worth — the shown copy is worthless
        // because nothing unowned pays, so the partner carries all of it.
        var registry = Registry("7D", "5H");

        Assert.Equal(3, registry.Multiplier(Card_("7D")));
        Assert.Equal(1, registry.Multiplier(Card_("5H")));
        Assert.Equal(1, registry.Multiplier(Card_("AS")));
    }

    [Fact]
    public void BothTurnedUpCardsDesignate()
    {
        var registry = Registry("5H", "KC");

        Assert.Equal(1, registry.Multiplier(Card_("5H")));
        Assert.Equal(1, registry.Multiplier(Card_("KC")));
    }

    [Fact]
    public void TwoTurnedUpCardsOfTheSameValueStillGiveAMultiplierOfOne()
    {
        // Both copies of the 5♥ turned up. Tripling is reserved for the permanent-plus-
        // turned-up overlap (RULES.md §4.1), not for stacking designators.
        var registry = Registry("5H", "5H");

        Assert.Equal(1, registry.Multiplier(Card_("5H")));
    }

    [Fact]
    public void TriplingIsTheCeilingEvenWhenBothCopiesOfAPermanentCardAreTurnedUp()
    {
        // Reachable about one round in 5,800, and it pays nobody: both copies are on the
        // table, so there is no partner left to own (RULES.md §4.1). The implementation must
        // merely not care, which is what this pins.
        var registry = Registry("7D", "7D");

        Assert.Equal(3, registry.Multiplier(Card_("7D")));
    }

    [Fact]
    public void ATurnedUpRedJokerTriplesTheRedJokersAndLeavesTheBlackOnesPayingOnce()
    {
        // RULES.md §9 #11, answered EXPERT in rev 20: "colour matters for jokers", so §4.2
        // applies unchanged and SameValueAs already computes it. What rev 21 changed is the
        // other half — a joker is a permanent money card, so a turned-up red joker is a
        // designation landing on one, and the black jokers keep paying once regardless.
        var registry = Registry("RJ");

        Assert.Equal(3, registry.Multiplier(Card_("RJ")));
        Assert.Equal(1, registry.Multiplier(Card_("BJ")));
    }

    [Fact]
    public void ASecondRegistryFromTheSameTurnedUpCardsAgreesExactly()
    {
        // The regression test for the retired non-idempotent re-marking bug: designation is a
        // pure function of the turned-up cards, so building it twice must change nothing.
        var turnedUp = Hands.Of("5H", "7D");
        var shoe = Deck.TwoDecks().Cards;

        var first = new MoneyCardRegistry(turnedUp);
        var multipliers = shoe.Select(first.Multiplier).ToList();
        var second = new MoneyCardRegistry(turnedUp);

        Assert.Equal(multipliers, shoe.Select(second.Multiplier));
        Assert.Equal(multipliers, shoe.Select(first.Multiplier));
    }

    [Fact]
    public void DesignationMutatesNoCard()
    {
        var turnedUp = Hands.Of("5H", "7D");
        var shoe = Deck.TwoDecks().Cards.ToList();
        var before = shoe.ToList();

        var registry = new MoneyCardRegistry(turnedUp);
        foreach (var card in shoe)
        {
            registry.Multiplier(card);
        }

        Assert.Equal(before, shoe);
        Assert.Equal(Hands.Of("5H", "7D"), turnedUp);
    }

    [Fact]
    public void TheRegistryCopiesTheTurnedUpCardsItIsGiven()
    {
        var turnedUp = new List<Card>(Hands.Of("5H"));
        var registry = new MoneyCardRegistry(turnedUp);

        turnedUp.Clear();
        turnedUp.Add(Card_("KC"));

        Assert.Equal(1, registry.Multiplier(Card_("5H")));
        Assert.Equal(0, registry.Multiplier(Card_("KC")));
    }

    [Fact]
    public void ANullTurnedUpListIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new MoneyCardRegistry(null!));

    // ── The jackpot (RULES.md §4.1, rev 21) ────────────────────────────────────────────────
    //
    // 🔥 The first rule in this game where a card's value depends on who holds what. Every
    // case below turns up the 7♦ and the A♠ from the shoe itself, because a partner is "the
    // copy that is not on the table" and that is an instance question (BUILD-PLAN §3.1).

    private static readonly IReadOnlyList<Card> Shoe = DeckBuilder.BuildTwoDecks();

    private static readonly PlayerId Ava = new(0);
    private static readonly PlayerId Bo = new(1);

    /// <summary>One of the two physical cards of this value in the shoe.</summary>
    private static Card Copy(Rank rank, Suit suit, int copy = 0) =>
        Shoe.Where(card => card.Rank == rank && card.Suit == suit).ElementAt(copy);

    private static Card JokerCopy(CardColor color, int copy = 0) =>
        Shoe.Where(card => card.IsJoker && card.Color == color).ElementAt(copy);

    private static CardOwnership Owning(params (Card Card, PlayerId Owner)[] records)
    {
        var ownership = new CardOwnership();

        foreach (var (card, owner) in records)
        {
            ownership.RecordFromDeck(card.Id, owner);
        }

        return ownership;
    }

    [Fact]
    public void OwningBothPartnersOfASevenAndAceTurnUpPaysFiveEach()
    {
        var registry = new MoneyCardRegistry([Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)]);

        var seven = Copy(Rank.Seven, Suit.Diamonds, 1);
        var ace = Copy(Rank.Ace, Suit.Spades, 1);
        var configuration = registry.ConfigurationOf(Owning((seven, Bo), (ace, Bo)), Shoe);

        Assert.Equal(Bo, configuration.OwnsBothPartners);
        Assert.Equal(MoneyCardRegistry.Jackpot, registry.Multiplier(seven, Bo, configuration));
        Assert.Equal(MoneyCardRegistry.Jackpot, registry.Multiplier(ace, Bo, configuration));
    }

    [Fact]
    public void PartnersSplitBetweenTwoPlayersPayThreeEach()
    {
        // The same designation, the same two cards. What moved is who the deck gave them to,
        // which is the whole of the rule.
        var registry = new MoneyCardRegistry([Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)]);

        var seven = Copy(Rank.Seven, Suit.Diamonds, 1);
        var ace = Copy(Rank.Ace, Suit.Spades, 1);
        var configuration = registry.ConfigurationOf(Owning((seven, Ava), (ace, Bo)), Shoe);

        Assert.Null(configuration.OwnsBothPartners);
        Assert.Equal(MoneyCardRegistry.Tripled, registry.Multiplier(seven, Ava, configuration));
        Assert.Equal(MoneyCardRegistry.Tripled, registry.Multiplier(ace, Bo, configuration));
    }

    [Fact]
    public void OneOwnedPartnerIsNotAJackpot()
    {
        var registry = new MoneyCardRegistry([Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)]);

        var seven = Copy(Rank.Seven, Suit.Diamonds, 1);
        var configuration = registry.ConfigurationOf(Owning((seven, Bo)), Shoe);

        Assert.Null(configuration.OwnsBothPartners);
        Assert.Equal(MoneyCardRegistry.Tripled, registry.Multiplier(seven, Bo, configuration));
    }

    [Fact]
    public void TheJackpotPaysTheOwnerAndNobodyElseHoldingATripledCard()
    {
        // Belt and braces on the predicate: the ×5 is asked of a card *and its owner*, so
        // handing the same card somebody else's name gets the ordinary answer back.
        var registry = new MoneyCardRegistry([Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)]);

        var seven = Copy(Rank.Seven, Suit.Diamonds, 1);
        var ace = Copy(Rank.Ace, Suit.Spades, 1);
        var configuration = registry.ConfigurationOf(Owning((seven, Bo), (ace, Bo)), Shoe);

        Assert.Equal(MoneyCardRegistry.Tripled, registry.Multiplier(seven, Ava, configuration));
    }

    [Fact]
    public void TheJackpotDoesNotSpillOntoTheOwnersOtherMoneyCards()
    {
        var registry = new MoneyCardRegistry([Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)]);

        var seven = Copy(Rank.Seven, Suit.Diamonds, 1);
        var ace = Copy(Rank.Ace, Suit.Spades, 1);
        var joker = JokerCopy(CardColor.Red);
        var configuration = registry.ConfigurationOf(Owning((seven, Bo), (ace, Bo), (joker, Bo)), Shoe);

        // The joker is permanent and undesignated, so it pays once whoever else Bo owns.
        Assert.Equal(1, registry.Multiplier(joker, Bo, configuration));
    }

    [Fact]
    public void WithNoOwnershipHandedInTheAnswerIsWhatTheValueAlonePays()
    {
        var registry = new MoneyCardRegistry([Copy(Rank.Seven, Suit.Diamonds), Copy(Rank.Ace, Suit.Spades)]);

        var seven = Copy(Rank.Seven, Suit.Diamonds, 1);

        Assert.Equal(MoneyCardRegistry.Tripled, registry.Multiplier(seven, Bo, MoneyOwnership.None));
        Assert.Equal(MoneyCardRegistry.Tripled, registry.Multiplier(seven));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoTripledJokersInOneHandAreNotAJackpot(bool oppositeColours)
    {
        // ⚠️ RULES.md §9 #32 — **open**, and this test is the fence. Rev 21 made jokers
        // permanent, which created turn-ups the ×5 was never stated about: a red joker and a
        // black one are two tripled values, and nobody has been asked whether owning both
        // partners pays five. **The narrow reading is what was said**, so widening it later
        // has to break this test rather than pass silently.
        var second = oppositeColours ? JokerCopy(CardColor.Black) : JokerCopy(CardColor.Red, 1);
        var registry = new MoneyCardRegistry([JokerCopy(CardColor.Red), second]);

        var redPartner = JokerCopy(CardColor.Red, 1);
        var blackPartner = JokerCopy(CardColor.Black, 1);
        var configuration = registry.ConfigurationOf(
            Owning((redPartner, Bo), (blackPartner, Bo)), Shoe);

        Assert.Null(configuration.OwnsBothPartners);
    }

    [Fact]
    public void ASevenAndAJokerTurnedUpAreNotAJackpotEither()
    {
        // The other combination rev 21 created and did not rule on (RULES.md §9 #32).
        var registry = new MoneyCardRegistry([Copy(Rank.Seven, Suit.Diamonds), JokerCopy(CardColor.Red)]);

        var seven = Copy(Rank.Seven, Suit.Diamonds, 1);
        var joker = JokerCopy(CardColor.Red, 1);
        var configuration = registry.ConfigurationOf(Owning((seven, Bo), (joker, Bo)), Shoe);

        Assert.Null(configuration.OwnsBothPartners);
        Assert.Equal(MoneyCardRegistry.Tripled, registry.Multiplier(seven, Bo, configuration));
        Assert.Equal(MoneyCardRegistry.Tripled, registry.Multiplier(joker, Bo, configuration));
    }

    [Fact]
    public void AnOrdinaryTurnUpHasNoJackpotHoweverMuchOnePlayerOwns()
    {
        var registry = new MoneyCardRegistry([Copy(Rank.Five, Suit.Hearts), Copy(Rank.King, Suit.Clubs)]);

        var configuration = registry.ConfigurationOf(
            Owning(
                (Copy(Rank.Five, Suit.Hearts, 1), Bo),
                (Copy(Rank.King, Suit.Clubs, 1), Bo),
                (Copy(Rank.Seven, Suit.Diamonds), Bo),
                (Copy(Rank.Ace, Suit.Spades), Bo)),
            Shoe);

        Assert.Null(configuration.OwnsBothPartners);
    }

    [Fact]
    public void BothCopiesOfTheSevenTurnedUpLeavesNoPartnerToWinAJackpotWith()
    {
        // Not the 7♦/A♠ pair at all, and there is nothing left to own either (RULES.md §4.1).
        var registry = new MoneyCardRegistry(
            [Copy(Rank.Seven, Suit.Diamonds, 0), Copy(Rank.Seven, Suit.Diamonds, 1)]);

        Assert.Null(registry.ConfigurationOf(new CardOwnership(), Shoe).OwnsBothPartners);
    }

    [Fact]
    public void TheConfigurationRejectsNulls()
    {
        var registry = Registry("7D", "AS");

        Assert.Throws<ArgumentNullException>(() => registry.ConfigurationOf(null!, Shoe));
        Assert.Throws<ArgumentNullException>(() => registry.ConfigurationOf(new CardOwnership(), null!));
    }
}
