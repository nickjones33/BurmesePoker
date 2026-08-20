using System.Globalization;

namespace BurmesePoker.Sim;

/// <summary>
/// A tournament as rows — a cell, its players, every corrected comparison, the ranking, and the
/// pairing diagnostic (BUILD-PLAN P17).
/// </summary>
/// <remarks>
/// <b>Five shapes in one file, because they answer five questions and the derived ones must be
/// re-derivable.</b> A reader who has only the ranking has to take the verdict's word for it; a
/// reader who has the cells can check the balance, recompute a margin, and see what the
/// correction cost. Same discipline as <see cref="NeighbourCsv"/>.
/// <para>
/// <b>Every number is formatted invariantly and nothing here is a clock</b>, so two runs of one
/// seed write byte-identical files — which is the acceptance test the packet stands on.
/// </para>
/// </remarks>
public static class TournamentCsv
{
    /// <summary>The whole report as rows.</summary>
    public static IEnumerable<string> Rows(TournamentReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var seed = report.Options.MasterSeed;
        var seats = report.Options.Seats;

        yield return "kind,master_seed,seats,cell,row,column,strategy,assignments,games,settled,"
            + "abandoned,seat_rounds,mean,error,independent_mean,independent_error,p_value,"
            + "holm_threshold,separated,survives";

        foreach (var cell in report.Pairs.Append(report.FreeForAll).Append(report.NullCell))
        {
            yield return string.Create(CultureInfo.InvariantCulture,
                $"cell,{seed},{seats},{cell.Label},{cell.Row},{cell.Column},,{cell.Assignments},"
                + $"{cell.Games},{cell.Settled},{cell.Abandoned},,"
                + $"{Number(cell.Margin.Mean)},{Number(cell.Margin.StandardError)},"
                + $"{Number(cell.IndependentMargin.Mean)},{Number(cell.IndependentMargin.StandardError)},,,,");

            foreach (var player in cell.Players)
            {
                yield return string.Create(CultureInfo.InvariantCulture,
                    $"player,{seed},{seats},{cell.Label},{cell.Row},{cell.Column},{player.Name},"
                    + $"{cell.Assignments},{cell.Games},{cell.Settled},{cell.Abandoned},"
                    + $"{string.Join(' ', player.SeatRounds)},"
                    + $"{Number(player.WinRate.Mean)},{Number(player.WinRate.StandardError)},"
                    + $"{Number(player.NetPerRound.Mean)},{Number(player.NetPerRound.StandardError)},,,,");
            }
        }

        foreach (var verdict in report.Verdicts)
        {
            yield return string.Create(CultureInfo.InvariantCulture,
                $"comparison,{seed},{seats},{verdict.Label},,,,,{verdict.Difference.Count},,,,"
                + $"{Number(verdict.Difference.Mean)},{Number(verdict.Difference.StandardError)},,,"
                + $"{Number(verdict.PValue)},{Number(verdict.Threshold)},"
                + $"{(verdict.Separated ? 1 : 0)},{(verdict.Survives ? 1 : 0)}");
        }

        foreach (var standing in report.Ranking)
        {
            yield return string.Create(CultureInfo.InvariantCulture,
                $"rank,{seed},{seats},,,,{standing.Name},,,,,{standing.Beaten} {standing.LostTo} "
                + $"{standing.Undecided},{Number(standing.MeanMargin)},,"
                + $"{Number(standing.FreeForAllWinRate.Mean)},{Number(standing.FreeForAllWinRate.StandardError)},,,,");
        }

        foreach (var check in report.Pairing)
        {
            yield return string.Create(CultureInfo.InvariantCulture,
                $"pairing,{seed},{seats},{check.Label},{check.Scope},,,,{check.Paired.Count},,,,"
                + $"{Number(check.Paired.Mean)},{Number(check.Paired.StandardError)},"
                + $"{Number(check.Independent.Mean)},{Number(check.Independent.StandardError)},"
                + $"{Number(check.Ratio)},,,");
        }

        yield return string.Create(CultureInfo.InvariantCulture,
            $"null-test,{seed},{seats},{report.NullCell.Label},,,,,{report.NullCell.Games},"
            + $"{report.NullCell.Settled},{report.NullCell.Abandoned},,"
            + $"{Number(1.0 / seats)},,,,,,{(report.NullTestHolds ? 1 : 0)},");
    }

    /// <summary>Writes them to a file.</summary>
    public static void WriteTo(string path, TournamentReport report) =>
        File.WriteAllLines(path, Rows(report));

    /// <summary>
    /// Fixed digits, invariant culture. <b>Not <c>0.#####</c></b>: a trailing-zero-trimming
    /// format writes a different number of characters for two numbers of the same precision,
    /// which is fine to read and awkward to diff.
    /// </summary>
    private static string Number(double value) =>
        double.IsFinite(value)
            ? value.ToString("0.000000", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
