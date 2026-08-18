using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Tests.Cards;

/// <summary>
/// The two identity notions (BUILD-PLAN §3.1). These tests exist because the retired 2023
/// code conflated them: two decks mean value-identical cards coexist, and the exact-cover
/// search needs instance identity while money-card designation needs value identity.
/// </summary>
public class CardTests
{
    private static readonly Card FiveOfHeartsA = Card.Ranked(new CardId(1), Rank.Five, Suit.Hearts);
    private static readonly Card FiveOfHeartsB = Card.Ranked(new CardId(55), Rank.Five, Suit.Hearts);
    private static readonly Card FiveOfDiamonds = Card.Ranked(new CardId(2), Rank.Five, Suit.Diamonds);

    [Fact]
    public void SameValueAs_IsTrueForTheTwoCopiesOfTheSameCard() =>
        Assert.True(FiveOfHeartsA.SameValueAs(FiveOfHeartsB));

    [Fact]
    public void SameValueAs_IsFalseAcrossSuits() =>
        Assert.False(FiveOfHeartsA.SameValueAs(FiveOfDiamonds));

    [Fact]
    public void SameValueAs_IsFalseAcrossRanks() =>
        Assert.False(FiveOfHeartsA.SameValueAs(Card.Ranked(new CardId(3), Rank.Six, Suit.Hearts)));

    [Fact]
    public void Equality_IsInstanceIdentity_SoTheTwoCopiesOfFiveOfHeartsDiffer()
    {
        Assert.False(FiveOfHeartsA == FiveOfHeartsB);
        Assert.NotEqual(FiveOfHeartsA, FiveOfHeartsB);
    }

    [Fact]
    public void Equality_IsTrueForTheSameInstance() =>
        Assert.True(FiveOfHeartsA == Card.Ranked(new CardId(1), Rank.Five, Suit.Hearts));

    [Fact]
    public void Ranked_TakesItsColorFromItsSuit()
    {
        Assert.Equal(CardColor.Red, FiveOfHeartsA.Color);
        Assert.Equal(CardColor.Black, Card.Ranked(new CardId(4), Rank.Five, Suit.Spades).Color);
    }

    [Fact]
    public void RankedCard_IsNotAJoker() => Assert.False(FiveOfHeartsA.IsJoker);

    [Fact]
    public void Joker_IsRanklessAndSuitless()
    {
        var joker = Card.Joker(new CardId(52), CardColor.Red);

        Assert.True(joker.IsJoker);
        Assert.Null(joker.Rank);
        Assert.Null(joker.Suit);
        Assert.Equal(CardColor.Red, joker.Color);
    }

    [Fact]
    public void Jokers_OfDifferentColorsAreNotValueEqual() =>
        Assert.False(Card.Joker(new CardId(52), CardColor.Red)
            .SameValueAs(Card.Joker(new CardId(53), CardColor.Black)));

    [Fact]
    public void Jokers_OfTheSameColorAreValueEqualButNotInstanceEqual()
    {
        var first = Card.Joker(new CardId(52), CardColor.Red);
        var second = Card.Joker(new CardId(106), CardColor.Red);

        Assert.True(first.SameValueAs(second));
        Assert.False(first == second);
    }

    [Fact]
    public void ToString_RendersRankAndSuit() => Assert.Equal("5♥", FiveOfHeartsA.ToString());

    [Fact]
    public void ToString_RendersJokerColor() =>
        Assert.Equal("🃏Red", Card.Joker(new CardId(52), CardColor.Red).ToString());
}
