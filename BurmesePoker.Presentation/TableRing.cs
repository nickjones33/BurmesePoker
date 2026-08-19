using BurmesePoker.Domain.Play;

namespace BurmesePoker.Presentation;

/// <summary>
/// Where a seat sits on the felt, once the table has been turned so that you are at the front
/// of it.
/// </summary>
/// <remarks>
/// <b>Names a position, never a coordinate.</b> The same rule as <see cref="CardDisplayState"/>
/// one layer along (§3.11 A2): this layer says <em>which side of the table</em> and the
/// stylesheet decides what that is in pixels — a grid area on a wide screen, a stacked list on
/// a narrow one. Nothing here knows how big anything is.
/// </remarks>
public enum RingSeat
{
    /// <summary>The front of the table — yours, whichever seat number you were dealt.</summary>
    Bottom,

    /// <summary>Your left hand, and the seat that plays after you.</summary>
    Left,

    /// <summary>Between your left and the far side.</summary>
    TopLeft,

    /// <summary>Directly opposite you.</summary>
    Top,

    /// <summary>Between the far side and your right.</summary>
    TopRight,

    /// <summary>Your right hand, and the seat that plays before you.</summary>
    Right
}

/// <summary>One seat, and where it sits once the table has been turned to face you.</summary>
/// <param name="Player">The seat.</param>
/// <param name="Place">Where on the felt it is drawn.</param>
/// <param name="IsYours">Whether it is the seat this view was turned for.</param>
public readonly record struct RingPlace(PlayerId Player, RingSeat Place, bool IsYours);

/// <summary>
/// The seating order, turned so that you are at the front of the table.
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>You are always at the bottom, and the others go clockwise from your left.</b> That is
/// not a rule of this game — it is the convention every card client shares, and players arrive
/// expecting it. The seat number you were dealt is bookkeeping; the seat you are sitting in is
/// the front one (BUILD-PLAN P13.5).
/// </para>
/// <para>
/// <b>It lives here rather than in a component because it is a pure function of the seating
/// and of which seat is yours</b> — no rendering technology in it at all, and therefore
/// unit-testable, which an expression buried in Razor is not. The same call
/// <see cref="CardOrder"/> made about display order.
/// </para>
/// <para>
/// ⚠️ <b>Clockwise from your left is also turn order.</b> The player who plays after you sits
/// on your left and the player who plays before you sits on your right, so the ring reads round
/// the table in the order the game is played — which is what makes
/// <see cref="Around"/>'s order the right one to put in the markup.
/// </para>
/// <para>
/// <b>A watcher has no seat</b> (<c>--seat 0</c>): the table is not turned at all and the
/// seating order is shown as dealt, with the first seat at the front. A client must say so
/// rather than let a watcher read the front seat as theirs — <see cref="RingPlace.IsYours"/>
/// is false for every seat in that case, which is the flag to say it by.
/// </para>
/// </remarks>
public static class TableRing
{
    /// <summary>
    /// Where the seats other than the front one go, for a table of that many seats.
    /// </summary>
    /// <remarks>
    /// Read left to right, they are the ring anticlockwise from the front seat — which, seen
    /// from the front seat, is clockwise from its left. Four seats fill the sides and the far
    /// end; five leave the far end empty and pair the corners; six fill everything.
    /// </remarks>
    private static readonly Dictionary<int, RingSeat[]> Ring = new()
    {
        [4] = [RingSeat.Left, RingSeat.Top, RingSeat.Right],
        [5] = [RingSeat.Left, RingSeat.TopLeft, RingSeat.TopRight, RingSeat.Right],
        [6] = [RingSeat.Left, RingSeat.TopLeft, RingSeat.Top, RingSeat.TopRight, RingSeat.Right]
    };

    /// <summary>Every table size this can lay out — four to six seats (RULES.md §2.1).</summary>
    public static IReadOnlyCollection<int> Sizes => Ring.Keys;

    /// <summary>
    /// The seats, turned so that <paramref name="you"/> is at the front — <b>the others
    /// clockwise from your left, and yours last</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>The order is the markup order, and it is chosen for the narrow screen.</b> A ring
    /// has no reading order until it has to stack, and when it stacks the front seat belongs at
    /// the bottom, next to the hand it goes with. Ending on yours gives that for free and keeps
    /// the tab and screen-reader order the same as the layout's — without an <c>order:</c>
    /// anywhere, which is how a ring of seats scrambles focus order (§3.11 B6).
    /// </remarks>
    /// <param name="seating">The seating order, which is also the turn order (RULES.md §3).</param>
    /// <param name="you">Your seat, or null when nobody at this table is you.</param>
    /// <exception cref="ArgumentException">
    /// The table is not one this can lay out, or <paramref name="you"/> is not sitting at it.
    /// </exception>
    public static IReadOnlyList<RingPlace> Around(IReadOnlyList<PlayerId> seating, PlayerId? you)
    {
        ArgumentNullException.ThrowIfNull(seating);

        if (!Ring.TryGetValue(seating.Count, out var others))
        {
            throw new ArgumentException(
                $"A table of {seating.Count} is not one this lays out; four to six is the game "
                + "(RULES.md §2.1).",
                nameof(seating));
        }

        // A watcher is not turned towards anybody, so the table is shown as it was dealt.
        var front = you is { } seat ? Seated(seating, seat) : 0;

        if (front < 0)
        {
            throw new ArgumentException($"{you} is not sitting at this table.", nameof(you));
        }

        var places = new List<RingPlace>(seating.Count);

        for (var step = 1; step < seating.Count; step++)
        {
            places.Add(new RingPlace(seating[(front + step) % seating.Count], others[step - 1], IsYours: false));
        }

        places.Add(new RingPlace(seating[front], RingSeat.Bottom, you is not null));

        return places;
    }

    /// <summary>Where a seat sits in the seating order, or -1 if it does not sit at this table.</summary>
    private static int Seated(IReadOnlyList<PlayerId> seating, PlayerId player)
    {
        for (var at = 0; at < seating.Count; at++)
        {
            if (seating[at] == player)
            {
                return at;
            }
        }

        return -1;
    }
}
