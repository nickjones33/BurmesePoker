namespace BurmesePoker.Sim;

/// <summary>
/// One comparison after the family it belongs to has been accounted for (BUILD-PLAN P17).
/// </summary>
/// <param name="Label">What was compared, e.g. <c>greedy vs simple</c>.</param>
/// <param name="Difference">The estimate and its interval, unchanged by the correction.</param>
/// <param name="Rank">Its position when the family is sorted by p-value, counting from 1.</param>
/// <param name="Threshold">
/// The level this comparison had to beat: <c>alpha / (m - rank + 1)</c>. The smallest p-value
/// faces plain Bonferroni and the largest faces the uncorrected alpha, which is what makes Holm
/// strictly more powerful than Bonferroni at the same guarantee.
/// </param>
/// <param name="Separated">Whether the raw 95% interval clears zero — the uncorrected verdict.</param>
/// <param name="Survives">Whether it survives the correction.</param>
public sealed record HolmVerdict(
    string Label,
    Measurement Difference,
    int Rank,
    double Threshold,
    bool Separated,
    bool Survives)
{
    /// <summary>The p-value the ranking was done on.</summary>
    public double PValue => Difference.PValue;
}

/// <summary>
/// The Holm–Bonferroni step-down correction, because <b>a round-robin manufactures findings</b>
/// (BUILD-PLAN P17).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Why this packet cannot ship without it.</b> <i>k</i> strategies is <i>k(k−1)/2</i>
/// comparisons — six at four rungs, fifteen at six. At a 95% interval each has one chance in
/// twenty of clearing zero by luck, so six comparisons are more likely than not to produce a
/// spurious "separated" over a few runs, and the whole point of the strategy programme is that
/// a rung is promoted on evidence. An uncorrected round-robin is a machine for promoting noise.
/// </para>
/// <para>
/// <b>Holm rather than Bonferroni</b> because it controls the same family-wise error rate and
/// never rejects less: sort the p-values ascending and test the <i>r</i>th against
/// <c>alpha / (m − r + 1)</c>, stopping at the first failure and failing everything after it.
/// The step-down is why the stop is total — a comparison cannot survive on its own threshold
/// when a more surprising one has already failed.
/// </para>
/// <para>
/// <b>The raw verdict is kept beside the corrected one and never replaced by it</b>, so a
/// reader can see what the correction cost. A number that is separated but does not survive is
/// not a finding; it is a reason to run more games.
/// </para>
/// </remarks>
public static class Holm
{
    /// <summary>The conventional family-wise error rate.</summary>
    public const double DefaultAlpha = 0.05;

    /// <summary>
    /// Corrects a family of comparisons, returned <b>in the order they were given</b> so a
    /// caller can print them in a matrix rather than in p-value order.
    /// </summary>
    public static IReadOnlyList<HolmVerdict> Correct(
        IReadOnlyList<(string Label, Measurement Difference)> comparisons,
        double alpha = DefaultAlpha)
    {
        ArgumentNullException.ThrowIfNull(comparisons);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alpha);

        var count = comparisons.Count;

        // Ordered by p-value, then by label — a family with two identical p-values must still
        // rank the same way on every run, or the CSV stops being byte-identical.
        var order = Enumerable.Range(0, count)
            .OrderBy(index => comparisons[index].Difference.PValue)
            .ThenBy(index => comparisons[index].Label, StringComparer.Ordinal)
            .ToList();

        var verdicts = new HolmVerdict[count];
        var stillRejecting = true;

        for (var rank = 0; rank < count; rank++)
        {
            var index = order[rank];
            var (label, difference) = comparisons[index];
            var threshold = alpha / (count - rank);

            stillRejecting = stillRejecting && difference.PValue <= threshold;

            verdicts[index] = new HolmVerdict(
                label,
                difference,
                rank + 1,
                threshold,
                difference.IsSeparatedFromZero,
                stillRejecting);
        }

        return verdicts;
    }
}
