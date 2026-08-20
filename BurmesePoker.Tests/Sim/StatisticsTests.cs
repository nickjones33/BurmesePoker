using BurmesePoker.Sim;

namespace BurmesePoker.Tests.Sim;

/// <summary>
/// The arithmetic every published number now goes through (packet P17): a mean over games, a
/// pairing that knows which games it is pairing, and a family-wise correction.
/// </summary>
/// <remarks>
/// <b>Nothing here plays a game.</b> These are the pieces that turn a run into an answer, and
/// they are worth testing on numbers whose right answer is known — a measured win rate cannot
/// tell you whether its own interval was computed correctly.
/// </remarks>
public class StatisticsTests
{
    [Fact]
    public void ARatioOverGamesIsTheTotalsDividedAndNotTheAverageOfThePerGameRatios()
    {
        // 🔥 These are two different numbers whenever the denominator moves between games, and
        // the difference is not academic: a strategy holds one seat in some games of a crossed
        // run and three in others, and the unweighted average over-weights the games where it
        // held fewest. Measured at four seats it flatters the stronger strategy by about a
        // point — the size of P16's whole seating effect. Adding an interval to a figure must
        // not change the figure.
        GameValue[] uneven = [new(1, 1, 1), new(2, 1, 3)];

        Assert.Equal(0.5, Measurement.Of(uneven).Mean, 12);          // 2 out of 4
        Assert.NotEqual(2.0 / 3, Measurement.Of(uneven).Mean, 12);   // and not (1 + 1/3) / 2

        // With the denominator held constant it reduces exactly to the per-game mean, interval
        // and all — which is why a rotated run reads the same either way.
        GameValue[] even = [new(1, 1, 2), new(2, 0, 2), new(3, 2, 2), new(4, 1, 2)];

        Assert.Equal(0.5, Measurement.Of(even).Mean, 12);
        Assert.Equal(
            Measurement.Of([0.5, 0.0, 1.0, 0.5]).StandardError,
            Measurement.Of(even).StandardError,
            12);
    }

    [Fact]
    public void AGameWithNoTrialsIsDroppedRatherThanCountedAsAZero()
    {
        // A strategy that never lost has no covered-when-losing, and a game it did not play has
        // no win rate. Padding either with a zero would put a confident interval on a fiction.
        GameValue[] some = [new(1, 3, 1), new(2, 0, 0), new(3, 5, 1)];

        var measured = Measurement.Of(some);

        Assert.Equal(2, measured.Count);
        Assert.Equal(4, measured.Mean, 12);
        Assert.Equal(0, Measurement.Of([new GameValue(1, 0, 0)]).Count);
    }

    [Fact]
    public void PairingJoinsOnTheSeedAGameWasDealtFromAndRefusesTwoRunsThatShareNone()
    {
        // ⚠️ The whole defence against a silently wrong pairing. Two cells of one master seed
        // deal game i from the same shoe and may be paired; two runs of different master seeds
        // share nothing, and pairing them by position would line up unrelated games and produce
        // a plausible number with no error at all.
        GameValue[] left = [new(11, 1), new(22, 0), new(33, 1)];
        GameValue[] right = [new(11, 0), new(22, 0), new(33, 0)];

        var paired = Measurement.Paired(left, right);

        Assert.Equal(3, paired.Count);
        Assert.Equal(2.0 / 3, paired.Mean, 12);

        GameValue[] elsewhere = [new(44, 1), new(55, 0)];

        Assert.Throws<ArgumentException>(() => Measurement.Paired(left, elsewhere));
    }

    [Fact]
    public void PairingUsesOnlyTheGamesBothSidesPlayedAndRefusesASampleThatNamesOneTwice()
    {
        // A strategy is not at every table of a fully crossed run, so an inner join is the
        // right answer — but a sample with the same game in it twice is a bug upstream, not a
        // sample, and it would quietly double that game's weight.
        GameValue[] left = [new(11, 1), new(22, 1), new(33, 1)];
        GameValue[] right = [new(22, 0), new(99, 0)];

        Assert.Equal(1, Measurement.Paired(left, right).Count);

        GameValue[] twice = [new(11, 1), new(11, 0)];

        Assert.Throws<ArgumentException>(() => Measurement.Paired(twice, left));
        Assert.Throws<ArgumentException>(() => Measurement.Paired(left, twice));
    }

    [Fact]
    public void PairingNarrowsWhenTheTwoSamplesMoveTogetherAndWidensWhenTheyMoveApart()
    {
        // 🔥 The finding P17's acceptance 3 was written the wrong way round about. Pairing is
        // not a synonym for a narrower interval: it measures the correlation that is really
        // there. Perfectly correlated samples differ by a constant and the interval collapses;
        // perfectly anti-correlated ones — which is what two strategies at one table are,
        // because exactly one seat declares — make it wider than the independent formula says.
        GameValue[] together = [new(1, 1), new(2, 2), new(3, 3), new(4, 4)];
        GameValue[] alongside = [new(1, 2), new(2, 3), new(3, 4), new(4, 5)];
        GameValue[] opposed = [new(1, 4), new(2, 3), new(3, 2), new(4, 1)];

        var independent = Measurement.Difference(Measurement.Of(together), Measurement.Of(alongside));

        Assert.Equal(0, Measurement.Paired(together, alongside).StandardError, 12);
        Assert.True(independent.StandardError > 0);

        var apart = Measurement.Paired(together, opposed);

        Assert.True(
            apart.StandardError
                > Measurement.Difference(Measurement.Of(together), Measurement.Of(opposed)).StandardError);
    }

    [Fact]
    public void APValueIsTheTailAreaAndAgreesWithTheIntervalAtTwoStandardErrors()
    {
        // The interval and the p-value must be the same statement said two ways, or a
        // comparison could be "separated" and still have a p-value nowhere near alpha.
        Assert.Equal(1, Normal.TwoSidedP(0), 6);
        Assert.Equal(0.3173, Normal.TwoSidedP(1), 3);
        Assert.Equal(0.0455, Normal.TwoSidedP(2), 3);
        Assert.Equal(0.0027, Normal.TwoSidedP(3), 3);

        var justSeparated = new Measurement(1000, 0.021, 0.01);
        var justNot = new Measurement(1000, 0.019, 0.01);

        Assert.True(justSeparated.IsSeparatedFromZero);
        Assert.False(justNot.IsSeparatedFromZero);
        Assert.True(justSeparated.PValue < Holm.DefaultAlpha);
        Assert.True(justNot.PValue > Holm.DefaultAlpha);
    }

    [Fact]
    public void HolmKeepsTheStrongestFindingAndDropsTheOnesTheFamilySizeCannotAfford()
    {
        // ⚠️ The reason the correction is not optional. Six comparisons at a 95% interval make
        // a spurious "separated" likelier than not over a few runs, and the strategy programme
        // promotes a rung on exactly this evidence.
        var comparisons = new (string, Measurement)[]
        {
            ("a vs b", new Measurement(2000, 0.10, 0.01)),   // p ~ 1e-23, survives anything
            ("c vs d", new Measurement(2000, 0.021, 0.01)),  // p ~ 0.036, separated, not enough
            ("e vs f", new Measurement(2000, 0.005, 0.01))   // p ~ 0.62, not even separated
        };

        var verdicts = Holm.Correct(comparisons);

        // Returned in the order they were given, so a caller can print a matrix.
        Assert.Equal(["a vs b", "c vs d", "e vs f"], verdicts.Select(verdict => verdict.Label));
        Assert.Equal([1, 2, 3], verdicts.Select(verdict => verdict.Rank));
        Assert.Equal([Holm.DefaultAlpha / 3, Holm.DefaultAlpha / 2, Holm.DefaultAlpha],
            verdicts.Select(verdict => verdict.Threshold));

        Assert.Equal([true, true, false], verdicts.Select(verdict => verdict.Separated));
        Assert.Equal([true, false, false], verdicts.Select(verdict => verdict.Survives));
    }

    [Fact]
    public void HolmStepsDownSoNothingSurvivesBehindAComparisonThatFailed()
    {
        // The step-down is what makes Holm a procedure rather than a per-row threshold: once
        // the r-th smallest p-value fails, everything larger fails with it, even where a row's
        // own threshold would have let it through.
        var comparisons = new (string, Measurement)[]
        {
            ("a vs b", new Measurement(2000, 0.022, 0.01)),  // p ~ 0.028, and alpha/3 is 0.0167
            ("c vs d", new Measurement(2000, 0.021, 0.01)),
            ("e vs f", new Measurement(2000, 0.020, 0.01))   // p ~ 0.046: clears a bare alpha alone
        };

        var verdicts = Holm.Correct(comparisons);

        Assert.All(verdicts, verdict => Assert.True(verdict.Separated));
        Assert.All(verdicts, verdict => Assert.False(verdict.Survives));
    }

    [Fact]
    public void AFamilyWithIdenticalPValuesIsRankedTheSameWayOnEveryRun()
    {
        // A CSV that is byte-identical between two runs of one seed cannot have a tie broken by
        // whatever order a sort happened to leave things in.
        var comparisons = new (string, Measurement)[]
        {
            ("z vs y", new Measurement(2000, 0.02, 0.01)),
            ("a vs b", new Measurement(2000, 0.02, 0.01))
        };

        var verdicts = Holm.Correct(comparisons);

        Assert.Equal(2, verdicts[0].Rank);
        Assert.Equal(1, verdicts[1].Rank);
    }
}
