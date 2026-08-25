namespace BurmesePoker.Sim;

/// <summary>
/// Ways of filling the seats that are not a rotation (BUILD-PLAN P16).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> <see cref="SimulationOptions.Seating"/> rotates one pattern, which
/// is the right default for "who wins more" and useless for any question about
/// <i>neighbours</i>: rotating <c>[A,B,A,B]</c> never produces a table where an A is fed by an
/// A. The pair <i>(my strategy, my upstream neighbour's)</i> is then not merely
/// under-sampled — half its cells are never played at all, and no amount of games recovers
/// them.
/// </para>
/// <para>
/// Both plans here are <b>ordered and total</b>: the games of a run cycle through the list, so
/// a run whose game count is a multiple of the list length has played every assignment the same
/// number of times. Balance is arithmetic rather than luck, which is what lets a cell count be
/// asserted instead of eyeballed.
/// </para>
/// </remarks>
public static class SeatingPlan
{
    /// <summary>
    /// Beyond this many assignments a "balanced" run is really a sampling scheme wearing the
    /// wrong name, and is refused rather than silently truncated.
    /// </summary>
    /// <remarks>
    /// 🔥 <b>Raised from 4,096 to 32,768 by P32 and to 65,536 by P45, and both times the number
    /// was a budget rather than a taste.</b> P32 moved the standing set to five seats
    /// (<c>7⁵ = 16,807</c>); P43 filled the raised cap exactly (<c>8⁵ = 32,768</c>); P45's ninth
    /// win-rate rung makes the ladder's crossing <c>9⁵ = 59,049</c>, so the cap either rises or
    /// the one cell the whole field sits down together in is refused. ⚠️ <b>Both raises were
    /// decided by measuring rather than by guessing</b>: P32 timed a full four-seat pass at
    /// <b>2,401 games in 51 s</b>, and P45 timed the nine-rung field before running it — see the
    /// packet — after the plan's own fear of "hours in that one cell" was recalled to be the
    /// same order-of-magnitude overestimate P32 corrected. <b>The full crossing is played and
    /// nothing is subsampled</b> (<c>STRATEGY.md</c> §11, "no silent caps").
    /// ⚠️ <b>What the cap is really protecting against is a run shorter than one pass</b>, in
    /// which "balanced" means "the first few thousand of them". Every cell of a tournament
    /// rounds its game count up to a whole number of passes
    /// (<see cref="TournamentOptions.GamesFor"/>); a hand-driven <c>--seating balanced</c> run
    /// does not, which is why the ceiling is kept rather than removed.
    /// </remarks>
    public const int MaximumAssignments = 65536;

    /// <summary>
    /// Every way of seating <paramref name="strategies"/> across <paramref name="seats"/> seats
    /// — all <c>strategies^seats</c> of them, in odometer order.
    /// </summary>
    /// <remarks>
    /// <b>This is the fully crossed design.</b> Under it a seat's strategy and its upstream
    /// neighbour's are independent, so grouping the rows by <c>(strategy, upstream_strategy)</c>
    /// compares like with like: conditioning on who fed me tells you nothing about the rest of
    /// the table. It is what re-runs P12's headline without the feeding arrangement baked into
    /// it.
    /// </remarks>
    public static IReadOnlyList<IReadOnlyList<Strategy>> Balanced(IReadOnlyList<Strategy> strategies, int seats)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        ArgumentOutOfRangeException.ThrowIfLessThan(seats, 1);

        if (strategies.Count == 0)
        {
            throw new ArgumentException("There is nobody to seat.", nameof(strategies));
        }

        var total = 1L;

        for (var seat = 0; seat < seats; seat++)
        {
            total *= strategies.Count;

            if (total > MaximumAssignments)
            {
                throw new ArgumentException(
                    $"{strategies.Count} strategies across {seats} seats is more than "
                    + $"{MaximumAssignments} assignments; use fewer of either, or a named plan.",
                    nameof(strategies));
            }
        }

        var assignments = new List<IReadOnlyList<Strategy>>((int)total);

        for (var index = 0; index < total; index++)
        {
            var assignment = new Strategy[seats];
            var digits = index;

            for (var seat = seats - 1; seat >= 0; seat--)
            {
                assignment[seat] = strategies[digits % strategies.Count];
                digits /= strategies.Count;
            }

            assignments.Add(assignment);
        }

        return assignments;
    }

    /// <summary>
    /// Every balanced assignment in which <b>each named strategy takes at least one seat</b> —
    /// the head-to-head cell of a tournament (BUILD-PLAN P17).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why the restriction.</b> <see cref="Balanced"/> across two strategies at four seats
    /// includes <c>[A,A,A,A]</c> and <c>[B,B,B,B]</c>, in which one of the two is not at the
    /// table at all. Those games say nothing about A against B, and worse, they break the
    /// pairing: a per-game difference between the two needs both of them to have played that
    /// game (<see cref="Measurement.Paired"/>). Dropping them makes every game of the cell a
    /// game both played, so the paired and unpaired means are over the same games and can be
    /// compared.
    /// </para>
    /// <para>
    /// <b>Still balanced by seat, which is the property that matters.</b> The surviving
    /// assignments are closed under rotation, so each strategy sits in each seat equally often
    /// — seat 0 opens and is the only seat offered the turned-up money card (P12), and that is
    /// removed by arithmetic rather than by hope. What varies is the <i>mix</i>: a two-strategy
    /// cell plays one-against-three, two-against-two and three-against-one, symmetrically.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<IReadOnlyList<Strategy>> HeadToHead(
        IReadOnlyList<Strategy> strategies, int seats)
    {
        ArgumentNullException.ThrowIfNull(strategies);

        if (strategies.Count > seats)
        {
            throw new ArgumentException(
                $"{strategies.Count} strategies cannot all be seated at {seats} seats.",
                nameof(strategies));
        }

        var names = strategies.Select(strategy => strategy.Name).ToHashSet(StringComparer.Ordinal);

        var assignments = Balanced(strategies, seats)
            .Where(assignment => assignment.Select(s => s.Name).ToHashSet(StringComparer.Ordinal).Count == names.Count)
            .ToList();

        return assignments;
    }

    /// <summary>
    /// One pattern, walked round the table — assignment <c>r</c> puts <c>pattern[0]</c> in
    /// seat <c>r</c> and keeps everyone in the same order behind them.
    /// </summary>
    /// <remarks>
    /// <b>The neighbourhood is the invariant.</b> A table is a directed cycle (RULES.md §5), so
    /// rotating an assignment leaves every <i>(upstream, me, downstream)</i> triple exactly as
    /// it was and changes only <b>which seat opens</b> — the one thing about a seat that is not
    /// a strategy (P12: seat 0 draws first and is the only seat offered the turned-up money
    /// card). Cycling a cell's games through all of them therefore averages the seat away
    /// without disturbing the thing being measured.
    /// </remarks>
    public static IReadOnlyList<IReadOnlyList<Strategy>> Rotations(IReadOnlyList<Strategy> pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        if (pattern.Count == 0)
        {
            throw new ArgumentException("There is nobody to seat.", nameof(pattern));
        }

        var rotations = new List<IReadOnlyList<Strategy>>(pattern.Count);

        for (var rotation = 0; rotation < pattern.Count; rotation++)
        {
            var assignment = new Strategy[pattern.Count];

            for (var seat = 0; seat < pattern.Count; seat++)
            {
                assignment[seat] = pattern[((seat - rotation) % pattern.Count + pattern.Count) % pattern.Count];
            }

            rotations.Add(assignment);
        }

        return rotations;
    }
}
