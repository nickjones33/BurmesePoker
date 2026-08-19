using BurmesePoker.Domain.Play;

namespace BurmesePoker.Sim;

/// <summary>
/// One seat's round: what it did, and what it cost or made.
/// </summary>
/// <param name="Seat">Seat index, which is also the turn order — seat 0 opens every round.</param>
/// <param name="Strategy">Who was sitting there. Half of the row's join key (BUILD-PLAN §3.8 item 4).</param>
/// <param name="Won">Whether this seat declared.</param>
/// <param name="Net">What the seat actually banked, positive to collect.</param>
/// <param name="Flat">
/// The round payment alone, which is flat and known from the winner (RULES.md §7.2).
/// </param>
/// <param name="SideBet">
/// Everything else, which is the money cards. <b>Derived by subtraction</b>, exactly as the
/// console's settlement report does it, so that the two halves always add up to what the
/// domain actually settled.
/// </param>
/// <param name="Covered">
/// How many of the thirteen held at the end melded — 13 for the winner, and how close a loser
/// came for everybody else.
/// </param>
/// <param name="Takes">Cards taken from the previous player's discards.</param>
/// <param name="Draws">Cards drawn blind, which are the only ones that confer ownership (RULES.md §4.4).</param>
/// <param name="ClaimOffers">How often the turned-up money card was offered (only ever to seat 0).</param>
/// <param name="Claims">How often it was taken.</param>
public sealed record SeatRow(
    int Seat,
    string Strategy,
    bool Won,
    int Net,
    int Flat,
    int SideBet,
    int Covered,
    int Takes,
    int Draws,
    int ClaimOffers,
    int Claims);

/// <summary>One played round of one game.</summary>
/// <param name="Round">Which round of the game, counting from 1.</param>
/// <param name="Turns">How many turns it ran, the winner's last one included.</param>
/// <param name="Reshuffles">How often the draw pile had to be rebuilt (RULES.md §5).</param>
/// <param name="Seats">Every seat's row, in seating order.</param>
public sealed record RoundRow(int Round, int Turns, int Reshuffles, IReadOnlyList<SeatRow> Seats);

/// <summary>One game: a table, a seed, and the rounds it got through.</summary>
/// <param name="Game">The game's index in the run.</param>
/// <param name="Seed">
/// The seed it was played from, derived from the master seed (<see cref="SeedSequence"/>) and
/// recorded so that a surprising game can be replayed on its own.
/// </param>
/// <param name="Seating">Strategy names by seat.</param>
/// <param name="Rounds">The rounds that finished.</param>
/// <param name="Abandoned">
/// Whether a round hit the turn cap, which ends the game where it stands. A result, not an
/// error: a strategy that stalls is telling you something.
/// </param>
/// <param name="Journal">
/// Every decision every seat made, when the run asked for one (BUILD-PLAN P14) — and null when
/// it did not, which is the default. <b>The seed above is a pointer and this is the record</b>
/// (§3.9): the seed replays this game only against today's strategies, while the journal
/// replays it against any.
/// </param>
public sealed record GameResult(
    int Game,
    int Seed,
    IReadOnlyList<string> Seating,
    IReadOnlyList<RoundRow> Rounds,
    bool Abandoned,
    GameJournal? Journal = null);
