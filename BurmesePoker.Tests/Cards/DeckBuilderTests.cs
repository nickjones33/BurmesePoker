using BurmesePoker.Domain.Cards;

namespace BurmesePoker.Tests.Cards;

/// <summary>The 108-card shoe: two decks shuffled together, jokers included (RULES.md §2).</summary>
public class DeckBuilderTests
{
    private static readonly IReadOnlyList<Card> Shoe = DeckBuilder.BuildTwoDecks();

    [Fact]
    public void BuildTwoDecks_Has108Cards() => Assert.Equal(108, Shoe.Count);

    [Fact]
    public void BuildTwoDecks_HasFourJokers_TwoOfEachColor()
    {
        var jokers = Shoe.Where(c => c.IsJoker).ToList();

        Assert.Equal(4, jokers.Count);
        Assert.Equal(2, jokers.Count(j => j.Color == CardColor.Red));
        Assert.Equal(2, jokers.Count(j => j.Color == CardColor.Black));
    }

    [Fact]
    public void BuildTwoDecks_HasEightOfEachRank()
    {
        foreach (var rank in CardText.AllRanks)
        {
            Assert.Equal(8, Shoe.Count(c => c.Rank == rank));
        }
    }

    [Fact]
    public void BuildTwoDecks_HasTwoOfEachRankAndSuitCombination()
    {
        foreach (var suit in CardText.AllSuits)
        {
            foreach (var rank in CardText.AllRanks)
            {
                Assert.Equal(2, Shoe.Count(c => c.Rank == rank && c.Suit == suit));
            }
        }
    }

    [Fact]
    public void BuildTwoDecks_HasThirteenRankedCardsPerSuitPerDeck() =>
        Assert.Equal(104, Shoe.Count(c => !c.IsJoker));

    [Fact]
    public void BuildTwoDecks_AssignsDistinctSequentialCardIds()
    {
        var ids = Shoe.Select(c => c.Id.Value).ToList();

        Assert.Equal(108, ids.Distinct().Count());
        Assert.Equal(Enumerable.Range(0, 108), ids.Order());
    }

    [Fact]
    public void BuildTwoDecks_GivesEveryRankedCardTheColorOfItsSuit() =>
        Assert.All(Shoe.Where(c => !c.IsJoker),
            c => Assert.Equal(CardText.ColorOf(c.Suit!.Value), c.Color));

    [Fact]
    public void BuildTwoDecks_ReturnsAFreshListEachCall() =>
        Assert.NotSame(DeckBuilder.BuildTwoDecks(), DeckBuilder.BuildTwoDecks());
}
