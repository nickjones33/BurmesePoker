namespace BurmesePoker.Sim;

/// <summary>
/// A number with an interval, because a number without one is not an answer (BUILD-PLAN P16).
/// </summary>
/// <param name="Count">How many independent observations it came from.</param>
/// <param name="Mean">The estimate.</param>
/// <param name="StandardError">The standard error of that estimate.</param>
/// <remarks>
/// <para>
/// <b>The independent observation is the game, not the turn and not the seat.</b> A seat's
/// takes within one round are as correlated as the hand it was dealt, so counting them as
/// independent trials would produce an interval several times too tight — the very error that
/// turns "no effect" into a finding. Every measurement here is therefore a mean over games,
/// each game contributing exactly one value.
/// </para>
/// <para>
/// <b>The interval is 95%, normal-approximate</b>, which is honest at the thousands of games a
/// cell is run at and would not be at a dozen. Nothing here is asserted in a test at a size
/// where it matters; the measured numbers come from the command line.
/// </para>
/// </remarks>
public readonly record struct Measurement(int Count, double Mean, double StandardError)
{
    /// <summary>Half-width of the 95% interval.</summary>
    public double Interval => 1.959963985 * StandardError;

    /// <summary>Whether the interval clears zero — i.e. whether there is anything to report.</summary>
    public bool IsSeparatedFromZero => Math.Abs(Mean) > Interval;

    /// <summary>The mean of a sample, with the standard error of that mean.</summary>
    public static Measurement Of(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return new Measurement(0, 0, 0);
        }

        var mean = values.Sum() / values.Count;

        if (values.Count == 1)
        {
            return new Measurement(1, mean, double.PositiveInfinity);
        }

        var variance = values.Sum(value => (value - mean) * (value - mean)) / (values.Count - 1);

        return new Measurement(values.Count, mean, Math.Sqrt(variance / values.Count));
    }

    /// <summary>
    /// <paramref name="left"/> minus <paramref name="right"/>, treating the two as independent.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Deliberately conservative.</b> Two cells of the neighbour experiment are played
    /// from the same master seed, so game 417 is dealt from the same shoe in both and the
    /// difference is really a paired one — whose true standard error is <i>smaller</i> than
    /// this. Adding the variances in the usual way therefore overstates the interval a little
    /// rather than understating it, which is the direction to be wrong in.
    /// </remarks>
    public static Measurement Difference(Measurement left, Measurement right) =>
        new(
            Math.Min(left.Count, right.Count),
            left.Mean - right.Mean,
            Math.Sqrt((left.StandardError * left.StandardError) + (right.StandardError * right.StandardError)));
}
