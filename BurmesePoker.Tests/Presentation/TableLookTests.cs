using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Presentation;

namespace BurmesePoker.Tests.Presentation;

/// <summary>
/// What the rules make public at the table, as one fold over the public events (packet P41):
/// every seat's discard pile may be looked through (RULES.md §5), and a card taken in the open
/// lies face up in front of its taker for as long as it stays in their hand (§5.2).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>Two of these tests are fences on recorded defaults, named for their §9 rows</b>: #49
/// (the claimed turn-up lies face up too) and #50 (the player chooses which physical copy to
/// throw, so the face-up copy stays when the concealed duplicate goes). If an expert answers
/// either question the other way, the fence is the change list.
/// </para>
/// <para>
/// ⚠️ <b>There is deliberately no test that a blind draw enters the fold, because there is no
/// call that could make it do so</b> — <c>TableLook</c> has no method for a draw, which is the
/// concealment rule as an absence. The narrators are held to it in <c>ConcealmentTests</c>.
/// </para>
/// </remarks>
public class TableLookTests
{
    private static readonly PlayerId Alice = new(1);
    private static readonly PlayerId Bob = new(2);

    private static readonly Card QueenOfClubs = Card.Ranked(new CardId(10), Rank.Queen, Suit.Clubs);
    private static readonly Card OtherQueenOfClubs = Card.Ranked(new CardId(64), Rank.Queen, Suit.Clubs);
    private static readonly Card SevenOfDiamonds = Card.Ranked(new CardId(20), Rank.Seven, Suit.Diamonds);
    private static readonly Card TwoOfSpades = Card.Ranked(new CardId(30), Rank.Two, Suit.Spades);

    [Fact]
    public void ADiscardGoesOnTopOfTheThrowersOwnPile()
    {
        var look = TableLook.Empty
            .Discarded(Alice, QueenOfClubs)
            .Discarded(Alice, TwoOfSpades)
            .Discarded(Bob, SevenOfDiamonds);

        Assert.Equal([QueenOfClubs, TwoOfSpades], look.PileOf(Alice));
        Assert.Equal([SevenOfDiamonds], look.PileOf(Bob));
    }

    [Fact]
    public void ATakenDiscardComesOffThePileAndLiesFaceUpInFrontOfItsTaker()
    {
        var look = TableLook.Empty
            .Discarded(Alice, QueenOfClubs)
            .TookDiscard(Bob, QueenOfClubs);

        Assert.Empty(look.PileOf(Alice));
        Assert.Equal([QueenOfClubs], look.FaceUpOf(Bob));
        Assert.True(look.IsFaceUp(QueenOfClubs.Id));
    }

    /// <summary>
    /// 🔥 <b>The fence on §9 #49's recorded default</b>: §5.2 was stated about a card picked up
    /// from a discard pile, and the opening claim (§4.5) is the one other open take — the most
    /// public take there is, so it lies face up too, until the expert says otherwise.
    /// </summary>
    [Fact]
    public void TheClaimedTurnUpLiesFaceUpUntilTheExpertSaysOtherwise()
    {
        var look = TableLook.Empty.MoneyCardClaimed(Alice, SevenOfDiamonds);

        Assert.Equal([SevenOfDiamonds], look.FaceUpOf(Alice));

        // And no pile moved: the card came off the table, not off anybody's discards.
        Assert.Empty(look.PileOf(Alice));
        Assert.Empty(look.PileOf(Bob));
    }

    /// <summary>
    /// 🔥 <b>The fence on §9 #50's recorded default</b>: two decks mean a seat can hold the
    /// face-up Q♣ and a concealed Q♣ at once, and the player chooses which physical copy to
    /// throw — which is what discarding by <c>CardId</c> already does. Throwing the concealed
    /// copy leaves the face-up card exactly where it lies, until the expert says otherwise.
    /// </summary>
    [Fact]
    public void TheFaceUpCopyStaysWhenTheConcealedDuplicateIsThrownUntilTheExpertSaysOtherwise()
    {
        var took = TableLook.Empty
            .Discarded(Alice, QueenOfClubs)
            .TookDiscard(Bob, QueenOfClubs);

        // Bob throws the *other* Q♣ — the one he was holding concealed. Same value, different
        // card, and the face-up copy visibly stays.
        var threwTheConcealedCopy = took.Discarded(Bob, OtherQueenOfClubs);
        Assert.Equal([QueenOfClubs], threwTheConcealedCopy.FaceUpOf(Bob));

        // And the discriminating half: throwing the face-up copy itself takes it off the table.
        var threwTheFaceUpCopy = took.Discarded(Bob, QueenOfClubs);
        Assert.Empty(threwTheFaceUpCopy.FaceUpOf(Bob));
    }

    [Fact]
    public void AFaceUpCardStopsBeingFaceUpWhenThatVeryCardIsThrown()
    {
        var look = TableLook.Empty
            .Discarded(Alice, QueenOfClubs)
            .TookDiscard(Bob, QueenOfClubs)
            .Discarded(Bob, QueenOfClubs);

        Assert.Empty(look.FaceUpOf(Bob));
        Assert.False(look.IsFaceUp(QueenOfClubs.Id));

        // And it is on Bob's own pile now, where the next seat may take it.
        Assert.Equal([QueenOfClubs], look.PileOf(Bob));
    }

    /// <summary>
    /// The reshuffle gathers every pile into the new draw pile (RULES.md §5) — but a face-up
    /// card is in a hand, not on a pile, so it stays exactly where it is (§5.2).
    /// </summary>
    [Fact]
    public void TheReshuffleSweepsThePilesAndLeavesTheFaceUpCardsInTheirHands()
    {
        var look = TableLook.Empty
            .Discarded(Alice, QueenOfClubs)
            .TookDiscard(Bob, QueenOfClubs)
            .Discarded(Bob, TwoOfSpades)
            .Discarded(Alice, SevenOfDiamonds)
            .DiscardsReshuffled();

        Assert.Empty(look.PileOf(Alice));
        Assert.Empty(look.PileOf(Bob));
        Assert.Equal([QueenOfClubs], look.FaceUpOf(Bob));
    }

    [Fact]
    public void ANewDealClearsEverything()
    {
        var look = TableLook.Empty
            .Discarded(Alice, QueenOfClubs)
            .TookDiscard(Bob, QueenOfClubs)
            .RoundStarted();

        Assert.Empty(look.PileOf(Alice));
        Assert.Empty(look.FaceUpOf(Bob));
    }
}
