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
/// <para>
/// <b><c>upstream_strategy</c> and <c>downstream_strategy</c> are join keys too</b>
/// (BUILD-PLAN P16). Both are pure functions of the seating this row's game already carries —
/// a table is a directed cycle, so seat <c>i</c> is fed by seat <c>i-1</c> and by nobody else
/// (RULES.md §5) — but §3.8 item 4's rule is that a row carries its own keys. A consumer that
/// has to rebuild the table to know who fed whom will eventually rebuild it wrong, and the
/// wrap-around at seat 0 is exactly where.
/// <b>They come from the seating and never from the run's options</b>, which is what lets a
/// replayed journal produce the same two columns (P14: <c>Replay.OptionsOf</c> reads only the
/// seed, the table size, the stakes and the names).
/// </para>
/// </remarks>
public static class CsvReport
{
    /// <summary>The header, then a row per seat per round, in game order.</summary>
    public static IEnumerable<string> Rows(SimulationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        yield return "master_seed,game,game_seed,round,seat,strategy,upstream_strategy,"
            + "downstream_strategy,won,net,flat,side_bet,"
            + "covered,takes,draws,claim_offers,claims,turns,reshuffles,claims_refused,abandoned_game";

        foreach (var game in report.Games)
        {
            var seats = game.Seating.Count;

            foreach (var round in game.Rounds)
            {
                foreach (var seat in round.Seats)
                {
                    var upstream = game.Seating[(seat.Seat - 1 + seats) % seats];
                    var downstream = game.Seating[(seat.Seat + 1) % seats];

                    yield return string.Create(CultureInfo.InvariantCulture,
                        $"{report.Options.MasterSeed},{game.Game},{game.Seed},{round.Round},{seat.Seat},"
                        + $"{seat.Strategy},{upstream},{downstream},"
                        + $"{(seat.Won ? 1 : 0)},{seat.Net},{seat.Flat},{seat.SideBet},"
                        + $"{seat.Covered},{seat.Takes},{seat.Draws},{seat.ClaimOffers},{seat.Claims},"
                        + $"{round.Turns},{round.Reshuffles},{round.ClaimsRefused},"
                        + $"{(game.Abandoned ? 1 : 0)}");
                }
            }
        }
    }

    /// <summary>Writes the rows to a file.</summary>
    public static void WriteTo(string path, SimulationReport report) =>
        File.WriteAllLines(path, Rows(report));
}
