namespace BurmesePoker.Sim;

/// <summary>
/// One game's contribution to one measurement — <b>a total and the trials it is out of</b>,
/// labelled with the seed that game was dealt from (BUILD-PLAN P17).
/// </summary>
/// <param name="GameSeed">
/// The game's own seed, from <see cref="SeedSequence.GameSeed"/>. <b>Not the game's index.</b>
/// </param>
/// <param name="Total">What the game contributed — wins, dollars, cards taken.</param>
/// <param name="Trials">
/// What it is out of — seat-rounds, rounds, acquisitions. <b>One by default</b>, which makes a
/// plain per-game number a special case of the ratio.
/// </param>
/// <remarks>
/// <para>
/// 🔥 <b>Why a pair and not a number.</b> A strategy holds a different number of seats in
/// different games of a fully crossed run, so "its win rate in this game" has a denominator
/// that moves. Averaging the per-game <i>ratios</i> unweighted is a different quantity from
/// dividing the totals, and the difference is not small: at four seats under balanced seating
/// it flatters the stronger strategy by <b>about a point</b> — as large as the whole seating
/// effect P16 measured. Every win rate this project has published is the ratio of the totals,
/// so keeping the pair is what lets a measurement carry an interval <i>without</i> quietly
/// becoming a different number (§3.8 item 4).
/// </para>
/// <para>
/// ⚠️ <b>The seed is the label because pairing is only valid between samples that share their
/// shoes.</b> Two cells of the same master seed deal game 417 from the same shoe whoever is
/// seated, so a difference between them is a paired one. Two cells of <i>different</i> master
/// seeds share nothing, and pairing them by index would silently line up unrelated games — a
/// mistake that produces a plausible number and no error. Labelling with the seed makes it
/// <b>inexpressible</b>: <see cref="Measurement.Paired"/> joins on the seed and refuses a join
/// that comes back empty. The index would have matched happily.
/// </para>
/// <para>
/// <b>A game with no trials is not a zero</b>, it is a game with nothing to say — a strategy
/// that never lost has no covered-when-losing. Such games are dropped rather than counted,
/// which is why a conditional series is shorter than its run.
/// </para>
/// </remarks>
public readonly record struct GameValue(int GameSeed, double Total, double Trials = 1);
