using System.Globalization;

namespace BurmesePoker.Sim;

/// <summary>
/// A run as rows — one per seat per round, each carrying its own join keys.
/// </summary>
/// <remarks>
/// <b>Every row repeats the master seed, the game, the game's own seed, the round and the
/// seat's strategy</b> (BUILD-PLAN §3.8 item 4). Without them a surprising number cannot be
/// reproduced or attributed, and a strategy comparison is folklore. It is also the exact text
/// two runs are compared on to show they are identical.
/// </remarks>
public static class CsvReport
{
    /// <summary>The header, then a row per seat per round, in game order.</summary>
    public static IEnumerable<string> Rows(SimulationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        yield return "master_seed,game,game_seed,round,seat,strategy,won,net,flat,side_bet,"
            + "covered,takes,draws,claim_offers,claims,turns,reshuffles,abandoned_game";

        foreach (var game in report.Games)
        {
            foreach (var round in game.Rounds)
            {
                foreach (var seat in round.Seats)
                {
                    yield return string.Create(CultureInfo.InvariantCulture,
                        $"{report.Options.MasterSeed},{game.Game},{game.Seed},{round.Round},{seat.Seat},"
                        + $"{seat.Strategy},{(seat.Won ? 1 : 0)},{seat.Net},{seat.Flat},{seat.SideBet},"
                        + $"{seat.Covered},{seat.Takes},{seat.Draws},{seat.ClaimOffers},{seat.Claims},"
                        + $"{round.Turns},{round.Reshuffles},{(game.Abandoned ? 1 : 0)}");
                }
            }
        }
    }

    /// <summary>Writes the rows to a file.</summary>
    public static void WriteTo(string path, SimulationReport report) =>
        File.WriteAllLines(path, Rows(report));
}
