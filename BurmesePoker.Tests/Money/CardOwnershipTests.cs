using System.Reflection;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Money;
using BurmesePoker.Domain.Play;

namespace BurmesePoker.Tests.Money;

/// <summary>
/// Ownership (RULES.md §4.4): the deck confers it, it is permanent, and it never transfers.
/// </summary>
public class CardOwnershipTests
{
    private static readonly PlayerId Ava = new(0);
    private static readonly PlayerId Bo = new(1);

    private static CardId Id(int value) => new(value);

    [Fact]
    public void ACardNeverRecordedHasNoOwner() =>
        Assert.Null(new CardOwnership().OwnerOf(Id(7)));

    [Fact]
    public void RecordingFromTheDeckConfersOwnership()
    {
        var ownership = new CardOwnership();

        ownership.RecordFromDeck(Id(7), Ava);

        Assert.Equal(Ava, ownership.OwnerOf(Id(7)));
    }

    [Fact]
    public void OwnershipIsPerCardInstanceNotPerValue()
    {
        // The two 5♥ in the shoe are separate physical cards with separate owners.
        var ownership = new CardOwnership();

        ownership.RecordFromDeck(Id(4), Ava);
        ownership.RecordFromDeck(Id(58), Bo);

        Assert.Equal(Ava, ownership.OwnerOf(Id(4)));
        Assert.Equal(Bo, ownership.OwnerOf(Id(58)));
    }

    [Fact]
    public void RecordingTheSameCardForTheSameOwnerAgainIsANoOp()
    {
        var ownership = new CardOwnership();

        ownership.RecordFromDeck(Id(7), Ava);
        ownership.RecordFromDeck(Id(7), Ava);

        Assert.Equal(Ava, ownership.OwnerOf(Id(7)));
        Assert.Single(ownership.Records);
    }

    [Fact]
    public void RecordingAnAlreadyOwnedCardForADifferentPlayerIsRejectedAndDoesNotTransfer()
    {
        // Ownership never transfers (RULES.md §4.4 rule 2). One physical card reaching two
        // players from the deck is a dealing bug, so it is surfaced rather than absorbed.
        var ownership = new CardOwnership();
        ownership.RecordFromDeck(Id(7), Ava);

        Assert.Throws<InvalidOperationException>(() => ownership.RecordFromDeck(Id(7), Bo));
        Assert.Equal(Ava, ownership.OwnerOf(Id(7)));
    }

    [Fact]
    public void ARedrawAfterAReshuffleLeavesTheCardWithWhoeverAcquiredItFirst()
    {
        // Alice is dealt the 7♦ and discards it; the draw pile runs out, the discards are
        // gathered, and Bo draws the same physical card. First acquisition wins — Alice keeps
        // it and Bo simply holds it (RULES.md §5).
        var ownership = new CardOwnership();
        ownership.RecordFromDeck(Id(7), Ava);

        Assert.False(ownership.TryRecordFromDeck(Id(7), Bo));
        Assert.Equal(Ava, ownership.OwnerOf(Id(7)));
        Assert.Single(ownership.Records);
    }

    [Fact]
    public void ACardNobodyOwnsIsRecordedForWhoeverDrewIt()
    {
        var ownership = new CardOwnership();

        Assert.True(ownership.TryRecordFromDeck(Id(7), Ava));
        Assert.Equal(Ava, ownership.OwnerOf(Id(7)));

        // Re-recording the same owner says yes and changes nothing.
        Assert.True(ownership.TryRecordFromDeck(Id(7), Ava));
        Assert.Single(ownership.Records);
    }

    [Fact]
    public void RecordsExposesEveryOwnershipForSettlement()
    {
        // P6 settles by walking these records, never by looking at hands.
        var ownership = new CardOwnership();
        ownership.RecordFromDeck(Id(4), Ava);
        ownership.RecordFromDeck(Id(58), Bo);
        ownership.RecordFromDeck(Id(12), Ava);

        Assert.Equal(3, ownership.Records.Count);
        Assert.Equal([Id(4), Id(12)], ownership.Records
            .Where(record => record.Value == Ava)
            .Select(record => record.Key)
            .OrderBy(id => id.Value));
    }

    [Fact]
    public void RecordsIsEmptyBeforeAnythingIsDealt() =>
        Assert.Empty(new CardOwnership().Records);

    [Fact]
    public void ThereIsNoWayToTransferClearOrRemoveAnOwnership()
    {
        // RULES.md §4.4 rule 2 is encoded in the shape of the API, not in a comment. A card
        // taken from the table or a discard pile is simply never recorded.
        var mutators = typeof(CardOwnership)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Select(method => method.Name)
            .Where(name =>
                name.Contains("Transfer", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Clear", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Remove", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(mutators);
    }
}
