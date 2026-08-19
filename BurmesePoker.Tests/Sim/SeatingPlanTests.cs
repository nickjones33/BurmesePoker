using BurmesePoker.Sim;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// The seating fix P16 had to make before it could ask its question (BUILD-PLAN P16).
/// </summary>
/// <remarks>
/// <b>Nothing here plays a game.</b> The defect these guard against is not a wrong number but a
/// cell that was never played at all — the rotation makes <i>(my strategy, the one before me)</i>
/// take one value per strategy, so the confound is in the design and no run size touches it.
/// That is a property of the assignment list, which is why it is checked as one.
/// </remarks>
public class SeatingPlanTests
{
    private static readonly Strategy Greedy = StrategyCatalog.Resolve("greedy");
    private static readonly Strategy Simple = StrategyCatalog.Resolve("simple");
    private static readonly Strategy Random = StrategyCatalog.Resolve("random");

    [Fact]
    public void TheRotationCanNeverSeatAStrategyBehindItself()
    {
        // The finding that made this packet start with the harness rather than with an
        // experiment: at two strategies and four seats the rotation has two states, and in
        // both of them every greedy is fed by a simple.
        var rotated = new SimulationOptions { Strategies = [Greedy, Simple], Seats = 4, Games = 100 };

        var pairs = Enumerable.Range(0, 100)
            .SelectMany(game => Pairs(rotated.Seating(game)))
            .Distinct()
            .ToList();

        Assert.Equal(2, pairs.Count);
        Assert.DoesNotContain(("greedy", "greedy"), pairs);
        Assert.DoesNotContain(("simple", "simple"), pairs);
    }

    [Fact]
    public void BalancedSeatingPlaysEveryPairAndPlaysThemEqually()
    {
        // The escape: with every assignment enumerated, a seat's strategy and its upstream
        // neighbour's are independent, so all four pairs exist and in equal numbers. That is
        // arithmetic rather than luck, which is what lets a cell count be asserted.
        var balanced = SeatingPlan.Balanced([Greedy, Simple], 4);

        Assert.Equal(16, balanced.Count);
        Assert.Equal(16, balanced.Select(seating => string.Join(",", seating.Select(s => s.Name))).Distinct().Count());

        var pairs = balanced.SelectMany(Pairs).GroupBy(pair => pair).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(4, pairs.Count);
        Assert.All(pairs.Values, count => Assert.Equal(16, count));
    }

    [Fact]
    public void ABalancedRunActuallySeatsWhatThePlanSaysItDoes()
    {
        // Options honour the list rather than quietly falling back to the rotation, and the
        // games cycle it in order — which is what makes balance a property of the game count.
        var balanced = SeatingPlan.Balanced([Greedy, Simple], 4);

        var options = new SimulationOptions
        {
            Strategies = [Greedy, Simple],
            Assignments = balanced,
            Seats = 4,
            Games = 32
        }.Validated();

        for (var game = 0; game < 32; game++)
        {
            Assert.Equal(balanced[game % 16], options.Seating(game));
        }
    }

    [Fact]
    public void RotatingAPatternMovesTheOpeningSeatAndNothingElse()
    {
        // A table is a directed cycle (RULES.md §5), so every rotation of one pattern is the
        // same neighbourhood seen from a different opening seat. That is what lets the
        // experiment average seat 0's advantages away without disturbing what it measures.
        var rotations = SeatingPlan.Rotations([Greedy, Simple, Random, Simple]);

        Assert.Equal(4, rotations.Count);

        for (var rotation = 0; rotation < 4; rotation++)
        {
            Assert.Equal("greedy", rotations[rotation][rotation].Name);
            Assert.Equal(Pairs(rotations[0]).Order(), Pairs(rotations[rotation]).Order());
        }
    }

    [Fact]
    public void ARunRefusesAnAssignmentItCouldNotTabulate()
    {
        // A seat filled by a strategy the run never named would play and then be missing from
        // every total, because the summary tabulates by the names in Strategies.
        var options = new SimulationOptions
        {
            Strategies = [Greedy],
            Assignments = [new[] { Greedy, Simple, Greedy, Greedy }],
            Seats = 4
        };

        Assert.Throws<ArgumentException>(() => options.Validated());

        // And an assignment that does not fill the table is a shorter round, not a seating.
        Assert.Throws<ArgumentException>(() => new SimulationOptions
        {
            Strategies = [Greedy],
            Assignments = [new[] { Greedy, Greedy }],
            Seats = 4
        }.Validated());
    }

    [Fact]
    public void BalancedSeatingRefusesToBecomeASamplingScheme()
    {
        // Six seats of four strategies is 4,096 assignments and six is 16,384 — past the point
        // where "balanced" would mean "the first few thousand of them".
        var ladder = StrategyCatalog.All;

        Assert.Equal(4096, SeatingPlan.Balanced(ladder, 6).Count);
        Assert.Throws<ArgumentException>(() => SeatingPlan.Balanced(ladder, 7));
    }

    /// <summary>Every <c>(seat's strategy, the strategy feeding it)</c> pair of one seating.</summary>
    private static List<(string Strategy, string Upstream)> Pairs(IReadOnlyList<Strategy> seating) =>
        [.. Enumerable.Range(0, seating.Count).Select(seat =>
            (seating[seat].Name, seating[(seat - 1 + seating.Count) % seating.Count].Name))];
}
