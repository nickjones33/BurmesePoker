using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Tests.Melds;

/// <summary>
/// The §7.1.1 table as data, and the one property of a meld it reads (packet P25).
/// </summary>
/// <remarks>
/// These are assertions about the rules document rather than about a search. If a line here
/// changes, a rule changed — and <c>RULES.md</c> §7.1.1 is where it has to change first.
/// </remarks>
public class TableRulesTests
{
    [Theory]
    [InlineData(2, 0, 0, false)]
    [InlineData(3, 2, 2, true)]
    [InlineData(4, 1, 1, true)]
    [InlineData(5, 0, 0, true)]
    [InlineData(6, 0, 0, true)]
    [InlineData(12, 0, 0, true)]
    public void TheTableIsTheOneInTheRules(
        int players, int series, int clean, bool setsAllowed)
    {
        var rules = TableRules.For(players);

        Assert.Equal(players, rules.Players);
        Assert.Equal(series, rules.RequiredSeries);
        Assert.Equal(clean, rules.RequiredCleanSeries);
        Assert.Equal(setsAllowed, rules.SetsAllowed);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void PurityAttachesToTheRequiredSeriesAndToNothingElse(int players)
    {
        // RULES.md §7.1: the two counts move together — nought and nought, one and one, two
        // and two. A surplus series need not be clean (§9 #28), so the clean count can never
        // run ahead of the series count.
        var rules = TableRules.For(players);

        Assert.Equal(rules.RequiredSeries, rules.RequiredCleanSeries);
    }

    [Fact]
    public void OnlyFiveOrMoreLeavesThePartitionAlone()
    {
        Assert.True(TableRules.For(2).ConstrainsThePartition);
        Assert.True(TableRules.For(3).ConstrainsThePartition);
        Assert.True(TableRules.For(4).ConstrainsThePartition);
        Assert.False(TableRules.For(5).ConstrainsThePartition);
        Assert.False(TableRules.For(6).ConstrainsThePartition);
    }

    [Fact]
    public void ATableSmallerThanTwoIsNotAGame()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TableRules.For(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => TableRules.For(0));
    }

    // ---- Meld.IsClean ------------------------------------------------------------------------

    [Fact]
    public void ARunWithNoJokerInItIsClean()
    {
        var run = OnlyMeld(Hands.Of("2D", "3D", "4D"));

        Assert.Equal(MeldKind.Run, run.Kind);
        Assert.True(run.IsClean);
    }

    [Fact]
    public void ARunCarryingAJokerIsNotClean()
    {
        // 2♦ 3♦ with the joker standing in for the 4♦ — a legal series, and not a clean one.
        var run = OnlyMeld(Hands.Of("2D", "3D", "RJ"));

        Assert.Equal(MeldKind.Run, run.Kind);
        Assert.Equal(1, run.JokerCount);
        Assert.False(run.IsClean);
    }

    [Fact]
    public void ASetIsNeverCleanBecauseCleanlinessIsAboutSeries()
    {
        var set = OnlyMeld(Hands.Of("9H", "9S", "9C"));

        Assert.Equal(MeldKind.Set, set.Kind);
        Assert.Equal(0, set.JokerCount);
        Assert.False(set.IsClean);
    }

    [Fact]
    public void AnAllJokerMeldIsASeriesAndIsNeverACleanOne()
    {
        // RULES.md §6.1, §9 #29 — the boundary case the two answers were reached on
        // independently. Three jokers laid down together are a series, so they can discharge
        // a *surplus* series; every slot is a substitute, so they can never discharge a
        // required one.
        var meld = OnlyMeld(Hands.Of("RJ", "BJ", "RJ"));

        Assert.Equal(MeldKind.Run, meld.Kind);
        Assert.Equal(3, meld.JokerCount);
        Assert.False(meld.IsClean);
    }

    /// <summary>The single meld three cards make, when they make exactly one.</summary>
    private static Meld OnlyMeld(IReadOnlyList<Card> hand) =>
        Assert.Single(MeldCandidates.For(hand));
}
