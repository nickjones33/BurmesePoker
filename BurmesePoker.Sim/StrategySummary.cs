namespace BurmesePoker.Sim;

/// <summary>
/// Everything one strategy did, across every seat it sat in.
/// </summary>
/// <remarks>
/// Counts and sums only — <b>every average is derived on the way out</b>, from integers added
/// in game order. Accumulating doubles instead would make the totals depend on the order the
/// games finished in, and a parallel run finishes them in whatever order it likes.
/// </remarks>
public sealed record StrategySummary(
    string Name,
    int Rounds,
    int Wins,
    int Net,
    int Flat,
    int SideBet,
    long TurnsWhenWon,
    long CoveredWhenLost,
    int Takes,
    int Draws,
    int ClaimOffers,
    int Claims)
{
    /// <summary>Share of the rounds this strategy went out in.</summary>
    public double WinRate => Rounds == 0 ? 0 : (double)Wins / Rounds;

    /// <summary>Money banked per round played, the two halves together.</summary>
    public double NetPerRound => Rounds == 0 ? 0 : (double)Net / Rounds;

    /// <summary>The money-card side bet alone, per round.</summary>
    public double SideBetPerRound => Rounds == 0 ? 0 : (double)SideBet / Rounds;

    /// <summary>How long its winning rounds ran, in turns around the whole table.</summary>
    public double TurnsToWin => Wins == 0 ? 0 : (double)TurnsWhenWon / Wins;

    /// <summary>How many of the thirteen melded when it lost — how close it came.</summary>
    public double CoveredWhenLosing => Rounds == Wins ? 0 : (double)CoveredWhenLost / (Rounds - Wins);

    /// <summary>Share of its cards taken from a discard pile rather than drawn blind.</summary>
    public double TakeRate => Takes + Draws == 0 ? 0 : (double)Takes / (Takes + Draws);

    /// <summary>Share of the offered turned-up money cards it took (RULES.md §4.5).</summary>
    public double ClaimRate => ClaimOffers == 0 ? 0 : (double)Claims / ClaimOffers;
}

/// <summary>The running totals behind a <see cref="StrategySummary"/>.</summary>
internal sealed class StrategyTally(string name)
{
    private int _rounds;
    private int _wins;
    private int _net;
    private int _flat;
    private int _sideBet;
    private long _turnsWhenWon;
    private long _coveredWhenLost;
    private int _takes;
    private int _draws;
    private int _claimOffers;
    private int _claims;

    internal void Add(SeatRow seat, int turns)
    {
        _rounds++;
        _net += seat.Net;
        _flat += seat.Flat;
        _sideBet += seat.SideBet;
        _takes += seat.Takes;
        _draws += seat.Draws;
        _claimOffers += seat.ClaimOffers;
        _claims += seat.Claims;

        if (seat.Won)
        {
            _wins++;
            _turnsWhenWon += turns;
        }
        else
        {
            _coveredWhenLost += seat.Covered;
        }
    }

    internal StrategySummary Summarise() => new(
        name, _rounds, _wins, _net, _flat, _sideBet,
        _turnsWhenWon, _coveredWhenLost, _takes, _draws, _claimOffers, _claims);
}
