using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Melds;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>
/// Plays one game and writes down what happened. Nothing here is shared with another game.
/// </summary>
/// <remarks>
/// <para>
/// <b>The domain is asked for nothing it does not already offer</b> (BUILD-PLAN §3.8): who won
/// and for how much comes off <see cref="RoundResult"/>, how the cards were taken off the
/// observer, how close the losers were off the round's own <see cref="TableState"/>, and how
/// often the money card was declined off the seat decorator. There is no statistic in the
/// domain and there is no call back into it to collect one.
/// </para>
/// <para>
/// <b><see cref="Replay"/> is the same method with its seats swapped</b> (BUILD-PLAN P14). A
/// journalled game plays back by seating <see cref="JournalPlayerAgent"/> everywhere and
/// building the rows from the same code, so a replay's CSV is comparable to the original's
/// line for line — which is what makes "it replays identically" a checkable claim rather than
/// an assertion.
/// </para>
/// </remarks>
public static class GameRunner
{
    /// <summary>Plays game <paramref name="game"/> of a run.</summary>
    public static GameResult Play(SimulationOptions options, int game)
    {
        ArgumentNullException.ThrowIfNull(options);

        var seed = SeedSequence.GameSeed(options.MasterSeed, game);
        var seating = options.Seating(game);
        var names = seating.Select(strategy => strategy.Name).ToArray();
        var players = Enumerable.Range(0, options.Seats).Select(seat => new PlayerId(seat)).ToArray();

        var recorders = players.ToDictionary(
            player => player,
            player => new SeatRecorder(
                seating[player.Value].Create(SeedSequence.SeatSeed(seed, player.Value)),
                options.TurnCap,
                options.CountLockBites));

        // The journal wraps the recorder rather than the other way round, so what is written
        // down is the answer the strategy actually gave (BUILD-PLAN P14).
        var journal = options.Journal is { } fidelity ? new GameJournalBuilder(fidelity) : null;

        return Run(
            players,
            recorders,
            names,
            options.Stakes,
            seed,
            options.RoundsPerGame,
            journal,
            game,
            masterSeed: options.MasterSeed);
    }

    /// <summary>
    /// Plays a journalled game back, seat by seat, from what was written down.
    /// </summary>
    /// <remarks>
    /// <b>It plays <see cref="JournalHeader.Rounds"/> rounds and no more.</b> A game abandoned
    /// mid-round leaves that round's decisions in the file; they are a record of what happened,
    /// not something to play back, and the header says where the settled rounds stop.
    /// </remarks>
    public static GameResult Replay(GameJournal journal, int game)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var header = journal.Header;
        var players = header.Players;
        var names = header.Seats.Select(seat => seat.Strategy).ToArray();

        // The journal bounds the replay by running out, so the harness cap is not the thing
        // stopping it; the recorder is here for the claim tallies the CSV wants.
        var recorders = players.ToDictionary(
            player => player,
            player => new SeatRecorder(new JournalPlayerAgent(journal, player), int.MaxValue));

        return Run(
            players,
            recorders,
            names,
            header.Stakes,
            header.Seed,
            header.Rounds,
            journal: null,
            game: header.Game ?? game,
            masterSeed: header.MasterSeed,
            abandoned: header.Abandoned);
    }

    private static GameResult Run(
        IReadOnlyList<PlayerId> players,
        IReadOnlyDictionary<PlayerId, SeatRecorder> recorders,
        IReadOnlyList<string> names,
        Stakes stakes,
        int seed,
        int rounds,
        GameJournalBuilder? journal,
        int game,
        int? masterSeed = null,
        bool abandoned = false)
    {
        var observer = new SimObserver(players);

        var seats = recorders.ToDictionary(seat => seat.Key, IPlayerAgent (seat) => seat.Value);

        var match = new MatchEngine(
            players,
            journal is null ? seats : JournalingAgent.Wrap(seats, journal),
            stakes,
            new Random(seed),
            observer);

        var played = new List<RoundRow>(rounds);

        for (var round = 0; round < rounds; round++)
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
                return Result(game, seed, names, played, abandoned: true, journal, players, stakes, masterSeed);
            }

            played.Add(Row(stakes, players, names, record, observer, recorders));
        }

        return Result(game, seed, names, played, abandoned, journal, players, stakes, masterSeed);
    }

    private static GameResult Result(
        int game,
        int seed,
        IReadOnlyList<string> names,
        IReadOnlyList<RoundRow> rounds,
        bool abandoned,
        GameJournalBuilder? journal,
        IReadOnlyList<PlayerId> players,
        Stakes stakes,
        int? masterSeed) =>
        new(game, seed, names, rounds, abandoned, journal?.Build(new JournalHeader(
            Seed: seed,
            Seats: [.. players.Select((player, seat) => new JournalSeat(player, names[seat]))],
            Stakes: stakes,
            Rounds: rounds.Count,
            Fidelity: journal.Fidelity,
            MasterSeed: masterSeed,
            Game: game,
            Abandoned: abandoned)));

    private static RoundRow Row(
        Stakes stakes,
        IReadOnlyList<PlayerId> players,
        IReadOnlyList<string> names,
        RoundRecord record,
        SimObserver observer,
        IReadOnlyDictionary<PlayerId, SeatRecorder> recorders)
    {
        var result = record.Result;
        var table = record.Table;
        // §7.2 step 1 as amended by §7.3: what a loser pays is the round value only when the
        // winner declared holding a joker. Splitting the net at the wrong place would post the
        // jokerless bonus to the side-bet column, where every money measurement reads it.
        var payment = Settlement.RoundPayment(
            stakes, TableRules.For(players.Count), result.Jokerless);
        var winnings = payment * (players.Count - 1);

        var seats = new List<SeatRow>(players.Count);

        for (var seat = 0; seat < players.Count; seat++)
        {
            var player = players[seat];
            var won = player == result.Winner;
            var net = result.Payouts[player];
            var flat = won ? winnings : -payment;

            seats.Add(new SeatRow(
                Seat: seat,
                Strategy: names[seat],
                Won: won,
                Net: net,
                Flat: flat,
                SideBet: net - flat,
                Covered: PartialCover.Best(table.SeatOf(player).Hand).CoveredCount,
                Takes: observer.TakesBy(player),
                Draws: observer.DrawsBy(player),
                ClaimOffers: recorders[player].ClaimOffers,
                Claims: recorders[player].Claims,
                DiscardsChosen: recorders[player].DiscardsChosen,
                RestrictedTurns: recorders[player].RestrictedTurns,
                LockBites: recorders[player].LockBites));
        }

        return new RoundRow(
            result.Round, result.Turns, observer.Reshuffles, observer.Refusals, seats, result.Jokerless);
    }
}
