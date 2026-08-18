using BurmesePoker.Domain.Money;

namespace BurmesePoker.Tests.Money;

/// <summary>
/// The two stakes fixed at the start of a game (RULES.md §4.3).
/// </summary>
public class StakesTests
{
    [Fact]
    public void TheStandardStakesAreFiveAndOne()
    {
        // The ratio is what balances the side-bet against the round prize (RULES.md §4.3).
        Assert.Equal(5, Stakes.Standard.RoundValue);
        Assert.Equal(1, Stakes.Standard.MoneyCardValue);
    }

    [Fact]
    public void StakesCanBeSetToAnyPositiveAmounts()
    {
        var stakes = new Stakes(20, 5);

        Assert.Equal(20, stakes.RoundValue);
        Assert.Equal(5, stakes.MoneyCardValue);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(5, 0)]
    [InlineData(5, -1)]
    public void AStakeThatIsNotAPositiveAmountIsRejected(int roundValue, int moneyCardValue) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stakes(roundValue, moneyCardValue));
}
