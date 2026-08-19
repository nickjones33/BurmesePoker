using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>
/// Plays one game and writes down what happened. Nothing here is shared with another game.
/// </summary>
/// <remarks>
/// <b>The domain is asked for nothing it does not already offer</b> (BUILD-PLAN §3.8): who won
/// and for how much comes off <see cref="RoundResult"/>, how the cards were taken off the
/// observer, how close the losers were off the round's own <see cref="TableState"/>, and how
/// often the money card was declined off the seat decorator. There is no statistic in the
/// domain and there is no call back into it to collect one.
/// </remarks>
public static class GameRunner
{
    /// <summary>Plays game <paramref name="game"/> of a run.</summary>
    public static GameResult Play(SimulationOptions options, int game)
    {
        ArgumentNullException.ThrowIfNull(options);

        var seed = SeedSequence.GameSeed(options.MasterSeed, game);
        var seating = options.Seating(game);
        var players = Enumerable.Range(0, options.Seats).Select(seat => new PlayerId(seat)).ToArray();

        var recorders = players.ToDictionary(
            player => player,
            player => new SeatRecorder(seating[player.Value].Create(), options.TurnCap));
        var observer = new SimObserver(players);

        var match = new MatchEngine(
            players,
            recorders.ToDictionary(seat => seat.Key, IPlayerAgent (seat) => seat.Value),
            options.Stakes,
            new Random(seed),
            observer);

        var rounds = new List<RoundRow>(options.RoundsPerGame);

        for (var round = 0; round < options.RoundsPerGame; round++)
        {
            observer.BeginRound();

            foreach (var recorder in recorders.Values)
            {
                recorder.BeginRound();
            }

            RoundRecord record;

            try
            {
                record = match.PlayRound();
            }
            catch (RoundAbandonedException)
            {
                // The game stops where it stands: the banks are untouched by a round that
                // never settled, and playing on would mean re-using its round number.
                return new GameResult(game, seed, [.. seating.Select(strategy => strategy.Name)], rounds, true);
            }

            rounds.Add(Row(options, seating, record, observer, recorders));
        }

        return new GameResult(game, seed, [.. seating.Select(strategy => strategy.Name)], rounds, false);
    }

    private static RoundRow Row(
        SimulationOptions options,
        IReadOnlyList<Strategy> seating,
        RoundRecord record,
        SimObserver observer,
        IReadOnlyDictionary<PlayerId, SeatRecorder> recorders)
    {
        var result = record.Result;
        var table = record.Table;
        var winnings = options.Stakes.RoundValue * (options.Seats - 1);

        var seats = new List<SeatRow>(options.Seats);

        for (var seat = 0; seat < options.Seats; seat++)
        {
            var player = new PlayerId(seat);
            var won = player == result.Winner;
            var net = result.Payouts[player];
            var flat = won ? winnings : -options.Stakes.RoundValue;

            seats.Add(new SeatRow(
                Seat: seat,
                Strategy: seating[seat].Name,
                Won: won,
                Net: net,
                Flat: flat,
                SideBet: net - flat,
                Covered: PartialCover.Best(table.SeatOf(player).Hand).CoveredCount,
                Takes: observer.TakesBy(player),
                Draws: observer.DrawsBy(player),
                ClaimOffers: recorders[player].ClaimOffers,
                Claims: recorders[player].Claims));
        }

        return new RoundRow(result.Round, result.Turns, observer.Reshuffles, seats);
    }
}
