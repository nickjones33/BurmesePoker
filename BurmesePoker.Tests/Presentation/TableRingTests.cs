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

    /// <summary>Every table a round may be dealt for, from the floor to the ceiling.</summary>
    /// <remarks>
    /// ⚠️ <b>It read <c>[4, 5, 6]</c> until P70</b>, which was the engine's floor rather than the
    /// game's (RULES.md §10 #7). The floor is two now, so the lobby can open a table this had no
    /// layout for — and an unlaid-out table is an exception on the felt, not a smaller ring.
    /// </remarks>
    [Fact]
    public void EveryTableTheGameAllowsCanBeLaidOut()
    {
        Assert.Equal([2, 3, 4, 5, 6], TableRing.Sizes.Order());
        Assert.Equal(RoundEngine.MinimumPlayers, TableRing.Sizes.Min());
        Assert.Equal(RoundEngine.MaximumPlayers, TableRing.Sizes.Max());
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
        // ⚠️ Two-handed the two are one seat, so there is nothing to put on either side of you;
        // its own fact below says where that seat goes instead.
        if (seats == 2)
        {
            return;
        }

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

        // ⚠️ Asked of every seat rather than of the third: a two-handed table has no third seat,
        // and the claim was never about which one you were dealt.
        for (var at = 0; at < seats; at++)
        {
            var ring = TableRing.Around(seating, seating[at]);

            Assert.Equal(
                [.. Enumerable.Range(1, seats).Select(step => seating[(at + step) % seats])],
                ring.Select(place => place.Player));

            Assert.True(ring[^1].IsYours);
        }
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
        // ⚠️ Three seats was in this list until P70 laid it out; the sizes refused are the ones
        // no round can be dealt for (RULES.md §2.1).
        Assert.Throws<ArgumentException>(() => TableRing.Around(Seating(1), new PlayerId(0)));
        Assert.Throws<ArgumentException>(() => TableRing.Around(Seating(7), new PlayerId(1)));
    }

    [Fact]
    public void ASeatThatIsNotAtThisTableIsRefused()
    {
        Assert.Throws<ArgumentException>(() => TableRing.Around(Seating(4), new PlayerId(9)));
    }

    /// <summary>
    /// 🔥 <b>Two-handed the seat after you and the seat before you are the same seat</b> — the
    /// one table where that is true (RULES.md §5.1, Two-handed, which is why the mutual lock is
    /// cheap there) — so the felt puts your one opponent across from you rather than on a side.
    /// </summary>
    [Fact]
    public void TwoHandedYourOneOpponentFacesYouAcrossTheFelt()
    {
        var seating = Seating(2);

        foreach (var you in seating)
        {
            var ring = TableRing.Around(seating, you);
            var other = Assert.Single(ring, place => !place.IsYours);

            Assert.Equal(RingSeat.Top, other.Place);
            Assert.Equal(RingSeat.Bottom, Assert.Single(ring, place => place.IsYours).Place);
            Assert.DoesNotContain(ring, place => place.Place is RingSeat.Left or RingSeat.Right);
        }
    }

}
