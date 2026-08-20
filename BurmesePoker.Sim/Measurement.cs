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
/// turns "no effect" into a finding. Every measurement here is therefore taken over games,
/// each game contributing exactly one row.
/// </para>
/// <para>
/// ⚠️ <b>That does not make every measurement an unweighted average.</b> A game contributes a
/// total and the trials it is out of (<see cref="GameValue"/>), and the estimate is the totals
/// divided by the trials — a <i>ratio</i> over games, not a mean of ratios. Where the
/// denominator is constant the two are the same number; where it moves, as it does the moment a
/// strategy holds a different number of seats in different games, they differ by about a point
/// at four seats and only the ratio agrees with what this project has already published
/// (BUILD-PLAN P17, §3.8 item 4).
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

    /// <summary>
    /// How many standard errors the estimate sits from zero. Zero when there is nothing to say.
    /// </summary>
    public double Z =>
        Count < 2 ? 0
        : StandardError > 0 ? Mean / StandardError
        : Mean == 0 ? 0
        : double.PositiveInfinity;

    /// <summary>
    /// The two-sided p-value for "this is really zero" — what a family-wise correction needs,
    /// because an interval is not a common scale across comparisons (see <see cref="Normal"/>).
    /// </summary>
    public double PValue => Count < 2 ? 1 : Normal.TwoSidedP(Z);

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

    /// <summary>
    /// A ratio over games: the totals divided by the trials, with the standard error of that
    /// ratio (BUILD-PLAN P17).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔥 <b>The estimate is the ratio of the sums, not the average of the per-game ratios.</b>
    /// Those are two different numbers whenever the denominator moves between games — a
    /// strategy holds one seat in some games of a crossed run and three in others — and the
    /// unweighted average of ratios over-weights the games where it held fewest seats, which at
    /// four seats flatters a strong strategy by about a point. Every win rate this project has
    /// published is the ratio of the sums, so this is the one that keeps yesterday's numbers
    /// comparable while gaining an interval.
    /// </para>
    /// <para>
    /// <b>The interval still comes from the games</b> (see the remark on this type). It is the
    /// textbook ratio estimator: the residual of game <i>g</i> is
    /// <c>total − ratio × trials</c>, and the standard error is the standard error of those
    /// residuals divided by the mean number of trials a game. When every game has the same
    /// number of trials this reduces <b>exactly</b> to the standard error of the per-game mean,
    /// which is what makes the two readings of a rotated run agree to the last digit.
    /// </para>
    /// <para>
    /// <b>Games with no trials are dropped</b>, not counted as zero — see
    /// <see cref="GameValue"/>.
    /// </para>
    /// </remarks>
    public static Measurement Of(IReadOnlyList<GameValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var played = values.Where(value => value.Trials > 0).ToList();

        if (played.Count == 0)
        {
            return new Measurement(0, 0, 0);
        }

        var trials = played.Sum(value => value.Trials);
        var ratio = played.Sum(value => value.Total) / trials;

        if (played.Count == 1)
        {
            return new Measurement(1, ratio, double.PositiveInfinity);
        }

        var residuals = played.Sum(value =>
        {
            var residual = value.Total - (ratio * value.Trials);

            return residual * residual;
        });

        // The mean number of trials a game is what turns a residual in *totals* back into one
        // in the units of the ratio.
        var perGame = trials / played.Count;

        return new Measurement(
            played.Count,
            ratio,
            Math.Sqrt(residuals / (played.Count * (played.Count - 1.0))) / perGame);
    }

    /// <summary>
    /// <paramref name="left"/> minus <paramref name="right"/> <b>game by game</b>, joined on
    /// the seed each game was dealt from (BUILD-PLAN P17).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the honest difference and <see cref="Difference"/> is the fallback.</b> The
    /// harness deals game <i>i</i> from the same shoe in every cell of one master seed
    /// (<see cref="SeedSequence.GameSeed"/>), so two samples taken over the same run are not
    /// independent and the usual add-the-variances formula is not measuring what it says. The
    /// per-game difference measures it directly, and it moves the interval in <b>whichever
    /// direction the correlation actually points</b>:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Across cells</b> — the same strategy at two different tables — the shoes are
    /// shared and the correlation is positive, so pairing <i>narrows</i> the interval. That is
    /// free variance reduction, and it is what common random numbers are for.</item>
    /// <item><b>Within one cell</b> — two strategies at the <i>same</i> table — exactly one
    /// seat declares, so the two series are strongly <i>negatively</i> correlated and pairing
    /// <i>widens</i> the interval. ⚠️ Here the independent formula is not conservative but
    /// anti-conservative, understating a four-seat head-to-head margin by about a third.
    /// Measured in P17; see <c>docs/STRATEGY.md</c>.</item>
    /// </list>
    /// <para>
    /// <b>Both sides are ratios</b> (see <see cref="Of(IReadOnlyList{GameValue})"/>), so the
    /// difference is a difference of ratios and its residual is each side's residual expressed
    /// in the units of its own ratio. The estimate is computed over the <i>joined</i> games
    /// alone, so it is always the difference the interval belongs to.
    /// </para>
    /// <para>
    /// ⚠️ <b>It takes the per-game values and not two finished measurements, on purpose.</b>
    /// Two <see cref="Measurement"/>s have already thrown away which games they came from, so a
    /// signature over them cannot tell a legitimate pairing from two runs of unrelated seeds —
    /// and pairing those is silently wrong rather than loudly wrong. Joining on the seed makes
    /// the mistake fail instead: no shared game, no answer.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// If either side names a game twice, or if the two share no game at all.
    /// </exception>
    public static Measurement Paired(IReadOnlyList<GameValue> left, IReadOnlyList<GameValue> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var byGame = new Dictionary<int, GameValue>(right.Count);

        foreach (var value in right)
        {
            if (!byGame.TryAdd(value.GameSeed, value))
            {
                throw new ArgumentException(
                    $"Game seed {value.GameSeed} appears twice on the right; a sample is one value a game.",
                    nameof(right));
            }
        }

        var seen = new HashSet<int>(left.Count);
        var here = new List<GameValue>(Math.Min(left.Count, right.Count));
        var there = new List<GameValue>(here.Capacity);

        foreach (var value in left)
        {
            if (!seen.Add(value.GameSeed))
            {
                throw new ArgumentException(
                    $"Game seed {value.GameSeed} appears twice on the left; a sample is one value a game.",
                    nameof(left));
            }

            if (byGame.TryGetValue(value.GameSeed, out var other) && value.Trials > 0 && other.Trials > 0)
            {
                here.Add(value);
                there.Add(other);
            }
        }

        if (here.Count == 0)
        {
            throw new ArgumentException(
                "These two samples share no game, so there is nothing to pair. Two runs of "
                + "different master seeds cannot be compared game by game.",
                nameof(right));
        }

        var mine = Of(here);
        var yours = Of(there);

        if (here.Count == 1)
        {
            return new Measurement(1, mine.Mean - yours.Mean, double.PositiveInfinity);
        }

        var myTrials = here.Sum(value => value.Trials) / here.Count;
        var yourTrials = there.Sum(value => value.Trials) / there.Count;
        var squares = 0.0;

        for (var game = 0; game < here.Count; game++)
        {
            var difference =
                ((here[game].Total - (mine.Mean * here[game].Trials)) / myTrials)
                - ((there[game].Total - (yours.Mean * there[game].Trials)) / yourTrials);

            squares += difference * difference;
        }

        return new Measurement(
            here.Count,
            mine.Mean - yours.Mean,
            Math.Sqrt(squares / (here.Count * (here.Count - 1.0))));
    }
}
