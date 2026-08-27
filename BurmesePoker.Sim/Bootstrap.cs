namespace BurmesePoker.Sim;

/// <summary>
/// A percentile bootstrap interval, and the normal-approximate one it is checking.
/// </summary>
/// <param name="Point">The full-sample estimate — the same margin <see cref="Measurement.Paired"/> reports.</param>
/// <param name="Lower">The lower percentile of the resampled margins.</param>
/// <param name="Upper">The upper percentile of the resampled margins.</param>
/// <param name="Resamples">How many resamples the interval was read off.</param>
/// <param name="Normal">The normal-approximate 95% interval on the same games, for comparison.</param>
/// <remarks>
/// ⚠️ <b>The two intervals answer the same question two ways, and agreement is the finding.</b>
/// The normal interval assumes the per-game margin is roughly symmetric and light-tailed; money
/// a round is neither — a ×5 jackpot (RULES.md §4) is a rare, large, one-sided contribution — so
/// the bootstrap is what says whether the normal interval this project publishes on the separated
/// money cells is honest at these tails or whether it is understating them (review F6).
/// </remarks>
public readonly record struct BootstrapInterval(
    double Point,
    double Lower,
    double Upper,
    int Resamples,
    Measurement Normal)
{
    /// <summary>Half the normal interval's width — what the bootstrap interval is compared against.</summary>
    public double NormalInterval => Normal.Interval;

    /// <summary>The bootstrap interval's lower and upper distance from the point, as a ± pair.</summary>
    public (double Below, double Above) FromPoint => (Point - Lower, Upper - Point);

    /// <summary>
    /// Whether the normal 95% interval sits inside the bootstrap one to within a tenth of its own
    /// width — i.e. whether the normal interval is not materially tighter than the resampled one.
    /// </summary>
    /// <remarks>
    /// The bootstrap interval can be a little asymmetric on a skewed statistic, so the check is
    /// against the wider bootstrap side. A normal interval that reaches past the bootstrap's is
    /// the one worth flagging: it would be claiming a separation the tails do not support.
    /// </remarks>
    public bool NormalIsCovered
    {
        get
        {
            var slack = 0.1 * NormalInterval;

            return Point - NormalInterval >= Lower - slack && Point + NormalInterval <= Upper + slack;
        }
    }
}

/// <summary>
/// The bootstrap: resample the games with replacement, recompute the statistic, read the interval
/// off the resamples — 🔥 <b>P48's coverage check on the heaviest-tailed verdicts</b> (review F6).
/// </summary>
/// <remarks>
/// <para>
/// <b>The unit resampled is the game, exactly as it is the unit of independence everywhere else</b>
/// (<see cref="Measurement"/>). A resample draws whole games with replacement and recomputes the
/// ratio-of-sums margin over them, so the bootstrap distribution carries the same seat-weighting
/// and the same zero-sum coupling within a table that the point estimate does.
/// </para>
/// <para>
/// <b>Deterministic given its seed</b>, so a published bootstrap interval reproduces like every
/// other number here (BUILD-PLAN §3.9). Nothing in the domain or the runner changes; this is
/// arithmetic over the per-game series a cell already kept.
/// </para>
/// </remarks>
public static class Bootstrap
{
    /// <summary>Resamples enough that the 2.5% and 97.5% percentiles are themselves stable.</summary>
    public const int DefaultResamples = 10000;

    /// <summary>
    /// A percentile-bootstrap interval on the paired margin of <paramref name="left"/> over
    /// <paramref name="right"/> — the difference of ratios over the games the two share.
    /// </summary>
    /// <param name="left">One side's per-game (total, trials), labelled by seed.</param>
    /// <param name="right">The other side's, joined to <paramref name="left"/> on the seed.</param>
    /// <param name="resamples">How many times to resample. Defaults to <see cref="DefaultResamples"/>.</param>
    /// <param name="seed">The resampler's seed, so the interval reproduces.</param>
    /// <param name="alpha">The two-sided miss rate; 0.05 is a 95% interval.</param>
    /// <exception cref="ArgumentException">If the two share no game — the same guard as pairing.</exception>
    public static BootstrapInterval PairedMargin(
        IReadOnlyList<GameValue> left,
        IReadOnlyList<GameValue> right,
        int resamples = DefaultResamples,
        int seed = 20260826,
        double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ArgumentOutOfRangeException.ThrowIfLessThan(resamples, 1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alpha);

        var (here, there) = Join(left, right);
        var normal = Measurement.Paired(left, right);

        var margins = new double[resamples];
        var random = new Random(seed);
        var count = here.Count;

        // A reusable index buffer, filled with replacement each resample.
        var pick = new int[count];

        for (var trial = 0; trial < resamples; trial++)
        {
            for (var draw = 0; draw < count; draw++)
            {
                pick[draw] = random.Next(count);
            }

            margins[trial] = Margin(here, there, pick);
        }

        Array.Sort(margins);

        return new BootstrapInterval(
            normal.Mean,
            Percentile(margins, alpha / 2),
            Percentile(margins, 1 - (alpha / 2)),
            resamples,
            normal);
    }

    /// <summary>The difference of the two ratios of sums over the picked games.</summary>
    private static double Margin(
        IReadOnlyList<GameValue> here, IReadOnlyList<GameValue> there, int[] pick)
    {
        double leftTotal = 0, leftTrials = 0, rightTotal = 0, rightTrials = 0;

        foreach (var index in pick)
        {
            leftTotal += here[index].Total;
            leftTrials += here[index].Trials;
            rightTotal += there[index].Total;
            rightTrials += there[index].Trials;
        }

        var mine = leftTrials > 0 ? leftTotal / leftTrials : 0;
        var yours = rightTrials > 0 ? rightTotal / rightTrials : 0;

        return mine - yours;
    }

    /// <summary>
    /// The two series aligned on their shared games, both with positive trials — the same join
    /// <see cref="Measurement.Paired"/> makes, so the bootstrap runs on exactly its games.
    /// </summary>
    private static (List<GameValue> Here, List<GameValue> There) Join(
        IReadOnlyList<GameValue> left, IReadOnlyList<GameValue> right)
    {
        var byGame = new Dictionary<int, GameValue>(right.Count);

        foreach (var value in right)
        {
            byGame[value.GameSeed] = value;
        }

        var here = new List<GameValue>(Math.Min(left.Count, right.Count));
        var there = new List<GameValue>(here.Capacity);

        foreach (var value in left)
        {
            if (byGame.TryGetValue(value.GameSeed, out var other) && value.Trials > 0 && other.Trials > 0)
            {
                here.Add(value);
                there.Add(other);
            }
        }

        if (here.Count == 0)
        {
            throw new ArgumentException(
                "These two samples share no game, so there is nothing to bootstrap.", nameof(right));
        }

        return (here, there);
    }

    /// <summary>The <paramref name="fraction"/> percentile of a sorted array, linearly interpolated.</summary>
    private static double Percentile(double[] sorted, double fraction)
    {
        if (sorted.Length == 1)
        {
            return sorted[0];
        }

        var position = fraction * (sorted.Length - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);

        return sorted[lower] + ((position - lower) * (sorted[upper] - sorted[lower]));
    }
}
