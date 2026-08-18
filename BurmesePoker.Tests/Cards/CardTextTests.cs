using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Tests.Cards;

/// <summary>
/// Covers the display and ordering tables salvaged from the retired 2023 <c>Common.cs</c>
/// (BUILD-PLAN §1.2). The point of these is the joker re-expression: rank and suit are
/// nullable now, and <c>null</c> means joker.
/// </summary>
public class CardTextTests
{
    [Theory]
    [InlineData(Rank.Two, "2")]
    [InlineData(Rank.Ten, "10")]
    [InlineData(Rank.Jack, "J")]
    [InlineData(Rank.Queen, "Q")]
    [InlineData(Rank.King, "K")]
    [InlineData(Rank.Ace, "A")]
    public void DisplayCode_RendersRank(Rank rank, string expected) =>
        Assert.Equal(expected, CardText.DisplayCode(rank));

    [Fact]
    public void DisplayCode_RendersJokerForNullRank() =>
        Assert.Equal("🃏", CardText.DisplayCode(null));

    [Theory]
    [InlineData(Suit.Hearts, "♥")]
    [InlineData(Suit.Spades, "♠")]
    [InlineData(Suit.Clubs, "♣")]
    [InlineData(Suit.Diamonds, "♦")]
    public void DisplaySuit_RendersSuit(Suit suit, string expected) =>
        Assert.Equal(expected, CardText.DisplaySuit(suit));

    [Fact]
    public void DisplaySuit_RendersEmptyForNullSuit() =>
        Assert.Equal(string.Empty, CardText.DisplaySuit(null));

    [Theory]
    [InlineData(Suit.Hearts, CardColor.Red)]
    [InlineData(Suit.Diamonds, CardColor.Red)]
    [InlineData(Suit.Spades, CardColor.Black)]
    [InlineData(Suit.Clubs, CardColor.Black)]
    public void ColorOf_MatchesSuit(Suit suit, CardColor expected) =>
        Assert.Equal(expected, CardText.ColorOf(suit));

    [Fact]
    public void Order_RunsTwoLowToAceHigh()
    {
        Assert.Equal(0, CardText.Order(Rank.Two));
        Assert.Equal(12, CardText.Order(Rank.Ace));
        Assert.Equal(
            CardText.AllRanks.Select((_, i) => i),
            CardText.AllRanks.Select(r => CardText.Order(r)));
    }

    [Fact]
    public void Order_SortsJokerLast() =>
        Assert.Equal(13, CardText.Order(null));

    [Fact]
    public void RankValues_AreContiguousAndAceIsHigh()
    {
        // The run generator relies on adjacent ranks differing by exactly one (BUILD-PLAN §3.2).
        int[] values = CardText.AllRanks.Select(r => (int)r).ToArray();
        Assert.Equal(Enumerable.Range(2, 13), values);
        Assert.Equal(14, (int)Rank.Ace);
    }

    [Fact]
    public void AllSuits_HoldsTheFourSuitsAndNoJoker() =>
        Assert.Equal(Enum.GetValues<Suit>().OrderBy(s => s), CardText.AllSuits.OrderBy(s => s));

    [Theory]
    [InlineData("10", Rank.Ten)]
    [InlineData("T", Rank.Ten)]
    [InlineData("t", Rank.Ten)]
    [InlineData("a", Rank.Ace)]
    [InlineData("K", Rank.King)]
    public void ParseRank_AcceptsCodes(string code, Rank expected) =>
        Assert.Equal(expected, CardText.ParseRank(code));

    [Theory]
    [InlineData("🃏")]
    [InlineData("1")]
    [InlineData("")]
    public void ParseRank_RejectsAnythingElse(string code) =>
        Assert.Throws<ArgumentException>(() => CardText.ParseRank(code));
}
