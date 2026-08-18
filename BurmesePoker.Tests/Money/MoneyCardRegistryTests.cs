using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;

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
    public void SevenOfDiamondsAndAceOfSpadesAreMoneyCardsWithNothingTurnedUp()
    {
        var registry = Registry();

        Assert.Equal(1, registry.Multiplier(Card_("7D")));
        Assert.Equal(1, registry.Multiplier(Card_("AS")));
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
    public void AJokerIsNotAPermanentMoneyCard()
    {
        var registry = Registry();

        Assert.Equal(0, registry.Multiplier(Card_("RJ")));
        Assert.Equal(0, registry.Multiplier(Card_("BJ")));
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

        // Both 5♥, both 7♦, both A♠ — six cards, and nothing else.
        Assert.Equal(6, designated.Count);
        Assert.Equal(2, designated.Count(c => c.Rank == Rank.Five && c.Suit == Suit.Hearts));
    }

    [Fact]
    public void ATurnedUpSevenOfDiamondsDoublesIt()
    {
        var registry = Registry("7D", "5H");

        Assert.Equal(2, registry.Multiplier(Card_("7D")));
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
        // Both copies of the 5♥ turned up. Doubling is the ceiling (RULES.md §4.1) and it
        // is reserved for the permanent-plus-turned-up overlap, not for stacking designators.
        var registry = Registry("5H", "5H");

        Assert.Equal(1, registry.Multiplier(Card_("5H")));
    }

    [Fact]
    public void DoublingIsTheCeilingEvenWhenBothCopiesOfAPermanentCardAreTurnedUp()
    {
        var registry = Registry("7D", "7D");

        Assert.Equal(2, registry.Multiplier(Card_("7D")));
    }

    [Fact]
    public void ATurnedUpRedJokerDesignatesTheRedJokersAndNotTheBlackOnes()
    {
        // RULES.md §9 #11, unanswered. The safe default is §4.2 applied unchanged:
        // designate by SameValueAs, which for a joker discriminates on colour.
        var registry = Registry("RJ");

        Assert.Equal(1, registry.Multiplier(Card_("RJ")));
        Assert.Equal(0, registry.Multiplier(Card_("BJ")));
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
}
