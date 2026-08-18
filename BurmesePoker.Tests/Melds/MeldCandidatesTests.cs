using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Tests.Melds;

/// <summary>
/// The union of the two generators (packet P5). The only thing <see cref="MeldCandidates"/>
/// adds to a concatenation is de-duplication <i>across</i> them — see
/// <see cref="AMeldBothGeneratorsFindIsOneCandidateAndReadsAsARun"/>.
/// </summary>
public class MeldCandidatesTests
{
    [Fact]
    public void EveryRunAndEverySetIsOffered()
    {
        var hand = Hands.Of("2D", "3D", "4D", "9H", "9S", "9C");

        var candidates = MeldCandidates.For(hand);

        Assert.Equal(2, candidates.Count);
        Assert.Single(candidates, meld => meld.Kind == MeldKind.Run);
        Assert.Single(candidates, meld => meld.Kind == MeldKind.Set);
    }

    [Fact]
    public void TheUnionIsExactlyTheCardSetsTheTwoGeneratorsBetweenThemFind()
    {
        // A hand rich enough that both generators fire repeatedly and overlap.
        var hand = Hands.Of("7H", "8H", "9H", "9S", "9C", "9D", "RJ", "BJ");

        var expected = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());
        foreach (var meld in RunGenerator.Candidates(hand).Concat(SetGenerator.Candidates(hand)))
        {
            expected.Add([.. meld.CardIds]);
        }

        var actual = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());
        foreach (var meld in MeldCandidates.For(hand))
        {
            Assert.True(actual.Add([.. meld.CardIds]),
                $"{meld} consumes cards another candidate already consumes.");
        }

        Assert.True(expected.SetEquals(actual));
    }

    [Fact]
    public void AMeldBothGeneratorsFindIsOneCandidateAndReadsAsARun()
    {
        // {9D, 🃏, 🃏} is a run — the jokers play the 10D and JD — and equally a set, the
        // jokers playing two absent suits. One card set, so one candidate.
        var hand = Hands.Of("9D", "RJ", "BJ");

        Assert.Single(RunGenerator.Candidates(hand));
        Assert.Single(SetGenerator.Candidates(hand));

        var meld = Assert.Single(MeldCandidates.For(hand));

        Assert.Equal(MeldKind.Run, meld.Kind);
        Assert.True(meld.CardIds.SetEquals(hand.Select(card => card.Id)));
    }

    [Fact]
    public void AMeldOfNothingButJokersIsOneCandidateToo()
    {
        // Both generators emit it (RULES.md §9 #8, the unlimited-jokers default), and it is
        // the same three cards either way.
        var meld = Assert.Single(MeldCandidates.For(Hands.Of("RJ", "BJ", "RJ")));

        Assert.Equal(MeldKind.Run, meld.Kind);
        Assert.Equal(3, meld.Count);
    }

    [Fact]
    public void AHandWithNothingMeldableOffersNothing()
    {
        Assert.Empty(MeldCandidates.For(Hands.Of("2D", "5H", "9S", "KC")));
    }

    [Fact]
    public void AnEmptyHandOffersNothing() => Assert.Empty(MeldCandidates.For([]));
}
