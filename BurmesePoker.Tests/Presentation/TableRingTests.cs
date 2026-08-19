using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;

namespace BurmesePoker.Tests.Presentation;

/// <summary>
/// ✅ <b>P13.5 — you are at the front of the table, whichever seat you were dealt.</b>
/// </summary>
/// <remarks>
/// <para>
/// The rotation is a pure function of the seating and of which seat is yours, which is exactly
/// why it is here rather than in a component: <em>"a standard nobody can check is a wish"</em>,
/// and an expression buried in Razor is unreachable from a test. The same call
/// <c>CardOrder</c> made about display order in P13.1.
/// </para>
/// <para>
/// ⚠️ <b>It is asserted for every table size the game allows</b> (RULES.md §2.1), because the
/// layout has to work at four, five and six and the sizes do not share a ring.
/// </para>
/// </remarks>
public class TableRingTests
{
    private static IReadOnlyList<PlayerId> Seating(int seats) =>
        [.. Enumerable.Range(1, seats).Select(seat => new PlayerId(seat))];

    public static TheoryData<int> Sizes => new(TableRing.Sizes);

    /// <summary>Four, five and six — the tables this game is played at.</summary>
    [Fact]
    public void EveryTableTheGameAllowsCanBeLaidOut()
    {
        Assert.Equal([4, 5, 6], TableRing.Sizes.Order());
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void EverySeatIsPlacedExactlyOnceAndNoPlaceIsUsedTwice(int seats)
    {
        var seating = Seating(seats);

        foreach (var you in seating)
        {
            var ring = TableRing.Around(seating, you);

            Assert.Equal(seats, ring.Count);
            Assert.Equal(
                seating.OrderBy(seat => seat.Value),
                ring.Select(place => place.Player).OrderBy(seat => seat.Value));
            Assert.Equal(seats, ring.Select(place => place.Place).Distinct().Count());
        }
    }

    /// <summary>
    /// 🔥 The whole point of the type: whatever seat number you were dealt, your seat is the one
    /// at the front, and it is the only one flagged as yours.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sizes))]
    public void YouAreAtTheFrontWhicheverSeatYouWereDealt(int seats)
    {
        var seating = Seating(seats);

        foreach (var you in seating)
        {
            var ring = TableRing.Around(seating, you);
            var yours = Assert.Single(ring, place => place.IsYours);

            Assert.Equal(you, yours.Player);
            Assert.Equal(RingSeat.Bottom, yours.Place);
        }
    }

    /// <summary>
    /// The seat that plays after you is on your left and the one that plays before you is on
    /// your right — which is what makes the ring read clockwise in the order the game is played.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sizes))]
    public void TheSeatAfterYouIsOnYourLeftAndTheSeatBeforeYouIsOnYourRight(int seats)
    {
        var seating = Seating(seats);

        for (var at = 0; at < seats; at++)
        {
            var ring = TableRing.Around(seating, seating[at]);

            var left = Assert.Single(ring, place => place.Place == RingSeat.Left);
            var right = Assert.Single(ring, place => place.Place == RingSeat.Right);

            Assert.Equal(seating[(at + 1) % seats], left.Player);
            Assert.Equal(seating[(at - 1 + seats) % seats], right.Player);
        }
    }

    /// <remarks>
    /// ⚠️ <b>Markup order, and it is chosen for the narrow screen.</b> When the felt is too
    /// narrow to be a ring it stacks in this order, and your seat belongs at the bottom of the
    /// stack next to the hand it goes with. Nothing reorders it in CSS (§3.11 B6).
    /// </remarks>
    [Theory]
    [MemberData(nameof(Sizes))]
    public void TheOrderIsTurnOrderEndingWithYours(int seats)
    {
        var seating = Seating(seats);
        var ring = TableRing.Around(seating, seating[2]);

        Assert.Equal(
            [.. Enumerable.Range(1, seats).Select(step => seating[(2 + step) % seats])],
            ring.Select(place => place.Player));

        Assert.True(ring[^1].IsYours);
    }

    /// <summary>A watcher has no seat, so the table is not turned and nothing is theirs.</summary>
    [Theory]
    [MemberData(nameof(Sizes))]
    public void AWatcherIsShownTheTableAsItWasDealtAndNoSeatIsTheirs(int seats)
    {
        var seating = Seating(seats);
        var ring = TableRing.Around(seating, you: null);

        Assert.All(ring, place => Assert.False(place.IsYours));
        Assert.Equal(seating[0], Assert.Single(ring, place => place.Place == RingSeat.Bottom).Player);
    }

    /// <remarks>
    /// The layout is the same either way — a watcher is shown the same felt as the player in
    /// the first seat, and only <see cref="RingPlace.IsYours"/> tells them apart. That is what
    /// makes <em>"say so"</em> a one-flag job in the component.
    /// </remarks>
    [Fact]
    public void AWatcherSeesTheSameRingAsWhoeverIsInTheFirstSeat()
    {
        var seating = Seating(5);

        Assert.Equal(
            TableRing.Around(seating, seating[0]).Select(place => (place.Player, place.Place)),
            TableRing.Around(seating, you: null).Select(place => (place.Player, place.Place)));
    }

    [Fact]
    public void ATableThisCannotSeatIsRefusedRatherThanDrawnWrong()
    {
        Assert.Throws<ArgumentException>(() => TableRing.Around(Seating(3), new PlayerId(1)));
        Assert.Throws<ArgumentException>(() => TableRing.Around(Seating(7), new PlayerId(1)));
    }

    [Fact]
    public void ASeatThatIsNotAtThisTableIsRefused()
    {
        Assert.Throws<ArgumentException>(() => TableRing.Around(Seating(4), new PlayerId(9)));
    }
}
