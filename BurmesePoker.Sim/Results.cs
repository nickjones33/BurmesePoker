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
/// <param name="Claims">
/// How often this seat <b>asked</b> for it.
/// <para>
/// ⚠️ <b>Asked for, not got, and P28 is what made those two different.</b> A claim needs the
/// permission of the seat that plays before the claimer (RULES.md §4.5), so a claim counted here
/// may have been refused — <see cref="RoundRow.ClaimsRefused"/> is where that shows, and
/// <c>Claims − ClaimsRefused</c> is what was actually taken off the table.
/// </para>
/// </param>
/// <param name="DiscardsChosen">Discards this seat was asked to choose — the denominator of the two below.</param>
/// <param name="RestrictedTurns">
/// Of those, how many the feeding ban had taken a held card out of (RULES.md §5.1) — the lock was
/// <b>live</b>, which is not yet the lock doing anything.
/// </param>
/// <param name="LockBites">
/// Of those, how many the ban changed the seat's answer on — 🔥 <b>P31's mechanism variable</b>,
/// and zero in every cell that did not ask for it (it costs a second ranking).
/// </param>
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
    int Claims,
    int DiscardsChosen = 0,
    int RestrictedTurns = 0,
    int LockBites = 0);

/// <summary>One played round of one game.</summary>
/// <param name="Round">Which round of the game, counting from 1.</param>
/// <param name="Turns">How many turns it ran, the winner's last one included.</param>
/// <param name="Reshuffles">How often the draw pile had to be rebuilt (RULES.md §5).</param>
/// <param name="ClaimsRefused">
/// How often the opener's claim was vetoed by the seat before it (RULES.md §4.5) — <b>0 or 1</b>,
/// since only the opening turn may claim.
/// <para>
/// ⚠️ <b>A round-level number and not a seat's, on purpose.</b> A refusal is one seat stopping
/// another, so attributing it to either would make it read as something that seat <em>did</em>
/// rather than as something the table settled; BUILD-PLAN P29 wants it as a share of the claims
/// attempted, which is what the round has.
/// </para>
/// </param>
/// <param name="Seats">Every seat's row, in seating order.</param>
/// <param name="Jokerless">
/// Whether the winner declared with no joker anywhere in the thirteen, which RULES.md §7.3 pays
/// ×2 at two, three or four seats and ×3 at five or more.
/// <para>
/// ⚠️ <b>A round-level fact and not a seat's</b>, for the same reason
/// <paramref name="ClaimsRefused"/> is: it is a property of the round's settlement, and only one
/// seat could ever carry it. ⚠️ <b>Any rate measured off it is a floor rather than an
/// estimate</b> — RULES.md §9 #33 is open on whether a joker may be shed before the declaring
/// discard, and the safe default taken by P33 says it may not, so a hand that needed to throw
/// one a turn early cannot reach the bonus at all.
/// </para>
/// </param>
public sealed record RoundRow(
    int Round,
    int Turns,
    int Reshuffles,
    int ClaimsRefused,
    IReadOnlyList<SeatRow> Seats,
    bool Jokerless = false);

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
