namespace BurmesePoker.Sim;

/// <summary>
/// One strategy's run, <b>a value per game</b> rather than a total (BUILD-PLAN P17).
/// </summary>
/// <param name="Name">The strategy. Half of every row's join key (BUILD-PLAN §3.8 item 4).</param>
/// <param name="WinRate">Share of the seats it held in that game that declared.</param>
/// <param name="NetPerRound">What those seats banked, per round played.</param>
/// <param name="SideBetPerRound">The money-card half of it alone.</param>
/// <param name="TurnsToWin">How long its winning rounds ran. Only games it won have trials.</param>
/// <param name="CoveredWhenLosing">How close it came. Only games it lost have trials.</param>
/// <param name="TakeRate">Share of its acquisitions taken off a discard rather than drawn blind.</param>
/// <param name="ClaimRate">Share of the offered turned-up money cards taken (RULES.md §4.5).</param>
/// <param name="SeatRounds">
/// How many seat-rounds it played in each seat index. <b>The balance check</b>: seat 0 opens and
/// is the only seat offered the turned-up money card (P12), so an unbalanced run measures the
/// seat as much as the strategy.
/// </param>
/// <remarks>
/// <para>
/// <b>The game is the unit of independence, and this is where that is enforced</b> — see
/// <see cref="Measurement"/>. A strategy that holds three of four seats in one game contributes
/// <i>one</i> value to each series, averaged over those seats, not three. Counting the seats as
/// three trials would tighten every interval by a factor no amount of games earns back.
/// </para>
/// <para>
/// <b>Conditional series are shorter than the run, on purpose.</b> A game contributes to
/// <see cref="TurnsToWin"/> only if the strategy won one of its rounds, and to
/// <see cref="CoveredWhenLosing"/> only if it lost one. Padding them with zeroes would measure
/// something else entirely, and dropping the game keeps the seed label honest for pairing.
/// </para>
/// <para>
/// <b>Derived from the report and nothing else.</b> <see cref="Simulator"/> does not change and
/// neither does the domain: everything here is arithmetic over rows the run already produced,
/// which is §3.8's rule that statistics are collected rather than computed.
/// </para>
/// </remarks>
public sealed record StrategySeries(
    string Name,
    IReadOnlyList<GameValue> WinRate,
    IReadOnlyList<GameValue> NetPerRound,
    IReadOnlyList<GameValue> SideBetPerRound,
    IReadOnlyList<GameValue> TurnsToWin,
    IReadOnlyList<GameValue> CoveredWhenLosing,
    IReadOnlyList<GameValue> TakeRate,
    IReadOnlyList<GameValue> ClaimRate,
    IReadOnlyList<int> SeatRounds)
{
    /// <summary>How many games contributed anything at all.</summary>
    public int Games => WinRate.Count(value => value.Trials > 0);

    /// <summary>Whether the strategy sat in every seat the same number of times, to within one.</summary>
    public bool IsSeatBalanced =>
        SeatRounds.Count == 0 || SeatRounds.Max() - SeatRounds.Min() <= 1;

    /// <summary>Every strategy the run named, in the order it named them.</summary>
    public static IReadOnlyList<StrategySeries> Of(SimulationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builders = new Dictionary<string, Builder>(StringComparer.Ordinal);

        foreach (var strategy in report.Options.Strategies)
        {
            builders[strategy.Name] = new Builder(strategy.Name, report.Options.Seats);
        }

        foreach (var game in report.Games)
        {
            if (game.Rounds.Count == 0)
            {
                continue;
            }

            var tallies = new Dictionary<string, GameTally>(StringComparer.Ordinal);

            foreach (var round in game.Rounds)
            {
                foreach (var seat in round.Seats)
                {
                    if (!tallies.TryGetValue(seat.Strategy, out var tally))
                    {
                        tally = new GameTally();
                        tallies[seat.Strategy] = tally;
                    }

                    tally.Add(seat, round.Turns);
                }
            }

            foreach (var (name, tally) in tallies)
            {
                if (!builders.TryGetValue(name, out var builder))
                {
                    // A seated strategy the run never named would otherwise vanish from the
                    // report — the same failure SimulationOptions.Validated refuses up front.
                    throw new InvalidOperationException(
                        $"Game {game.Game} seated '{name}', which the run does not name.");
                }

                builder.Add(game.Seed, tally);
            }
        }

        return [.. report.Options.Strategies.Select(strategy => builders[strategy.Name].Build())];
    }

    /// <summary>One strategy's seat-rounds within one game.</summary>
    private sealed class GameTally
    {
        internal int Rounds { get; private set; }

        internal int Wins { get; private set; }

        internal int Net { get; private set; }

        internal int SideBet { get; private set; }

        internal long TurnsWhenWon { get; private set; }

        internal int Losses { get; private set; }

        internal long CoveredWhenLost { get; private set; }

        internal int Takes { get; private set; }

        internal int Draws { get; private set; }

        internal int ClaimOffers { get; private set; }

        internal int Claims { get; private set; }

        internal readonly List<int> Seats = [];

        internal void Add(SeatRow seat, int turns)
        {
            Rounds++;
            Net += seat.Net;
            SideBet += seat.SideBet;
            Takes += seat.Takes;
            Draws += seat.Draws;
            ClaimOffers += seat.ClaimOffers;
            Claims += seat.Claims;
            Seats.Add(seat.Seat);

            if (seat.Won)
            {
                Wins++;
                TurnsWhenWon += turns;
            }
            else
            {
                Losses++;
                CoveredWhenLost += seat.Covered;
            }
        }
    }

    private sealed class Builder(string name, int seats)
    {
        private readonly List<GameValue> _winRate = [];
        private readonly List<GameValue> _net = [];
        private readonly List<GameValue> _sideBet = [];
        private readonly List<GameValue> _turnsToWin = [];
        private readonly List<GameValue> _covered = [];
        private readonly List<GameValue> _takeRate = [];
        private readonly List<GameValue> _claimRate = [];
        private readonly int[] _seatRounds = new int[seats];

        internal void Add(int gameSeed, GameTally tally)
        {
            foreach (var seat in tally.Seats)
            {
                if (seat < _seatRounds.Length)
                {
                    _seatRounds[seat]++;
                }
            }

            // ⚠️ A total and the trials it is out of, never a ratio. A strategy holds one seat
            // in some games of a crossed run and three in others, and averaging the per-game
            // ratios unweighted is a different number from dividing the totals — by about a
            // point at four seats, which is the size of P16's whole seating effect. See
            // GameValue and Measurement.Of.
            _winRate.Add(new GameValue(gameSeed, tally.Wins, tally.Rounds));
            _net.Add(new GameValue(gameSeed, tally.Net, tally.Rounds));
            _sideBet.Add(new GameValue(gameSeed, tally.SideBet, tally.Rounds));
            _turnsToWin.Add(new GameValue(gameSeed, tally.TurnsWhenWon, tally.Wins));
            _covered.Add(new GameValue(gameSeed, tally.CoveredWhenLost, tally.Losses));
            _takeRate.Add(new GameValue(gameSeed, tally.Takes, tally.Takes + tally.Draws));
            _claimRate.Add(new GameValue(gameSeed, tally.Claims, tally.ClaimOffers));
        }

        internal StrategySeries Build() => new(
            name, _winRate, _net, _sideBet, _turnsToWin, _covered, _takeRate, _claimRate, _seatRounds);
    }
}
