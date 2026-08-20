namespace BurmesePoker.Sim;

/// <summary>
/// The one piece of statistics the harness has to compute rather than count: the tail area of
/// a normal distribution (BUILD-PLAN P17).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a p-value at all, when every figure already carries an interval.</b> An interval
/// answers "is this separated from zero" one comparison at a time, and a round-robin asks the
/// question <i>k(k−1)/2</i> times over. Correcting for that (see <see cref="Holm"/>) needs the
/// comparisons on a common scale, and the interval is not one — a difference of 2.0 ± 1.9 and
/// a difference of 0.2 ± 0.19 are equally "separated" and are not equally surprising.
/// </para>
/// <para>
/// <b>Normal, not Student's t</b>, for the same reason <see cref="Measurement"/>'s interval is:
/// a cell is thousands of games, where the two agree to more digits than anything here prints.
/// </para>
/// </remarks>
public static class Normal
{
    /// <summary>
    /// The two-sided tail area beyond <paramref name="z"/> standard errors — <c>erfc(|z|/√2)</c>.
    /// </summary>
    public static double TwoSidedP(double z)
    {
        if (double.IsNaN(z))
        {
            return 1;
        }

        return double.IsInfinity(z) ? 0 : Erfc(Math.Abs(z) / Math.Sqrt(2));
    }

    /// <summary>
    /// The complementary error function, by Abramowitz &amp; Stegun 7.1.26 — good to about
    /// 1.5e-7, which is several digits more than a p-value compared against 0.05 can use.
    /// </summary>
    private static double Erfc(double x)
    {
        const double P = 0.3275911;
        const double A1 = 0.254829592;
        const double A2 = -0.284496736;
        const double A3 = 1.421413741;
        const double A4 = -1.453152027;
        const double A5 = 1.061405429;

        var t = 1 / (1 + (P * x));
        var poly = t * (A1 + (t * (A2 + (t * (A3 + (t * (A4 + (t * A5))))))));

        return poly * Math.Exp(-x * x);
    }
}
