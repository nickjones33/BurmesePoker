using System.Globalization;

namespace BurmesePoker.Sim;

/// <summary>
/// The neighbour experiment as rows — one per cell, and one per contrast (BUILD-PLAN P16).
/// </summary>
/// <remarks>
/// <b>Two shapes in one file, because they answer two different questions.</b> A <c>cell</c>
/// row is what the focal seat did under one arrangement; an <c>effect</c> row is the difference
/// between two of them, which is the thing with a verdict attached. Keeping the cells means a
/// reader can re-derive every contrast — and check the balance the design claims — rather than
/// take the summary's word for it.
/// </remarks>
public static class NeighbourCsv
{
    /// <summary>Every cell, then every contrast, each carrying the master seed it came from.</summary>
    public static IEnumerable<string> Rows(NeighbourReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var seed = report.Options.MasterSeed;
        var focal = report.Options.Focal.Name;

        yield return "kind,master_seed,focal,arm,level,reference,games,settled,abandoned,seat_balance,"
            + "win_rate,win_rate_error,take_rate,take_rate_error,net_per_round,net_error,"
            + "covered_when_losing,level_win_rate";

        foreach (var cell in report.Cells)
        {
            yield return string.Create(CultureInfo.InvariantCulture,
                $"cell,{seed},{focal},{Arm(cell.Arm)},{cell.Level},,{cell.Games},{cell.Settled},"
                + $"{cell.Abandoned},{string.Join(' ', cell.FocalSeatGames)},"
                + $"{cell.WinRate.Mean:0.#####},{cell.WinRate.StandardError:0.#####},"
                + $"{cell.TakeRate.Mean:0.#####},{cell.TakeRate.StandardError:0.#####},"
                + $"{cell.NetPerRound.Mean:0.#####},{cell.NetPerRound.StandardError:0.#####},"
                + $"{cell.CoveredWhenLosing.Mean:0.#####},{cell.LevelWinRate.Mean:0.#####}");
        }

        foreach (var effect in report.Effects)
        {
            yield return string.Create(CultureInfo.InvariantCulture,
                $"effect,{seed},{focal},{Arm(effect.Arm)},{effect.Level},{effect.Reference},,,,,"
                + $"{effect.WinRate.Mean:0.#####},{effect.WinRate.StandardError:0.#####},"
                + $"{effect.TakeRate.Mean:0.#####},{effect.TakeRate.StandardError:0.#####},"
                + $"{effect.NetPerRound.Mean:0.#####},{effect.NetPerRound.StandardError:0.#####},,");
        }

        foreach (var level in report.Options.Levels.Where(level => level.Name != report.Options.Reference.Name))
        {
            var (winRate, takeRate) = report.Directional(level.Name);

            yield return string.Create(CultureInfo.InvariantCulture,
                $"directional,{seed},{focal},upstream-less-downstream,{level.Name},"
                + $"{report.Options.Reference.Name},,,,,"
                + $"{winRate.Mean:0.#####},{winRate.StandardError:0.#####},"
                + $"{takeRate.Mean:0.#####},{takeRate.StandardError:0.#####},,,,");
        }
    }

    /// <summary>Writes them to a file.</summary>
    public static void WriteTo(string path, NeighbourReport report) =>
        File.WriteAllLines(path, Rows(report));

    private static string Arm(Neighbour arm) => arm.ToString().ToLowerInvariant();
}
