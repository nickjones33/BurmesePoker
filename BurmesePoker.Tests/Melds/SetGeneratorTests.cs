using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Melds;

namespace BurmesePoker.Tests.Melds;

/// <summary>
/// Set candidate generation (packet P4). The rule that shapes every case here is
/// RULES.md §6.2: <b>duplicate suits are forbidden</b>, confirmed by Mya Lay, so a set holds
/// at most four cards.
/// </summary>
public class SetGeneratorTests
{
    // ---- the acceptance cases of BUILD-PLAN §5 P4 ----------------------------------------

    [Fact]
    public void ThreeDistinctSuitsOfOneRankAreOneSet()
    {
        var hand = Hands.Of("9H", "9S", "9D");

        var set = Assert.Single(SetGenerator.Candidates(hand));

        AssertConsumes(set, hand[0], hand[1], hand[2]);
        AssertIsAValidSet(set);
    }

    [Fact]
    public void AllFourSuitsYieldFourThreeCardSetsAndOneFourCardSet()
    {
        var hand = Hands.Of("9H", "9S", "9D", "9C");

        var candidates = SetGenerator.Candidates(hand);

        Assert.Equal(5, candidates.Count);
        Assert.Equal(4, candidates.Count(meld => meld.Count == 3));
        Assert.Single(candidates, meld => meld.Count == 4);
        Assert.All(candidates, AssertIsAValidSet);
    }

    [Fact]
    public void DuplicateSuitsAreNeverASet()
    {
        // The confirmed rule, and the one the rev 1 draft got backwards (RULES.md §6.2).
        Assert.Empty(SetGenerator.Candidates(Hands.Of("9H", "9H", "9S")));
    }

    [Fact]
    public void AJokerFillsAnAbsentSuit()
    {
        var hand = Hands.Of("9H", "9S", "RJ");

        var set = Assert.Single(SetGenerator.Candidates(hand));

        // One candidate, not two: the joker plays the club or the diamond, but it consumes
        // the same three cards either way, and identity is the card set.
        AssertConsumes(set, hand[0], hand[1], hand[2]);
        var jokerSlot = Assert.Single(set.Slots, slot => slot.IsSubstitute);
        Assert.Equal(Rank.Nine, jokerSlot.PlaysAs);
        Assert.DoesNotContain(jokerSlot.InSuit, new[] { Suit.Hearts, Suit.Spades });
    }

    [Fact]
    public void NoCandidateEverExceedsFourCards()
    {
        // Every rank held in every suit twice over, plus every joker — nothing here may
        // produce a five-card set, because there is no fifth suit to hold it.
        var hand = Hands.Of("9H", "9H", "9S", "9S", "9D", "9D", "9C", "9C",
                            "RJ", "BJ", "RJ", "BJ");

        Assert.All(SetGenerator.Candidates(hand),
            meld => Assert.InRange(meld.Count, SetGenerator.MinimumSize, SetGenerator.MaximumSize));
    }

    // ---- sets that are not sets ------------------------------------------------------------

    [Fact]
    public void TwoCardsAreNeverASet()
    {
        Assert.Empty(SetGenerator.Candidates(Hands.Of("9H", "9S")));
    }

    [Fact]
    public void MixedRanksAreNeverASet()
    {
        Assert.Empty(SetGenerator.Candidates(Hands.Of("9H", "10S", "JD")));
    }

    [Fact]
    public void AnEmptyHandProducesNoCandidates()
    {
        Assert.Empty(SetGenerator.Candidates(Hands.Of()));
    }

    // ---- duplicate copies (defect D4) ------------------------------------------------------

    [Fact]
    public void TwoCopiesOfOneSuitAreTwoCandidates_NotAWiderSet()
    {
        // Two decks, so both 9♥s can be held. Neither widens a set — each is its own way of
        // filling the heart position (BUILD-PLAN §5 P4).
        var hand = Hands.Of("9H", "9H", "9S", "9D");
        var (firstHeart, secondHeart, spade, diamond) = (hand[0], hand[1], hand[2], hand[3]);

        var candidates = SetGenerator.Candidates(hand);

        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, meld => Consumes(meld, firstHeart, spade, diamond));
        Assert.Contains(candidates, meld => Consumes(meld, secondHeart, spade, diamond));
    }

    [Fact]
    public void EachJokerInstanceIsItsOwnCandidate()
    {
        var hand = Hands.Of("9H", "9S", "RJ", "BJ");

        var candidates = SetGenerator.Candidates(hand);

        Assert.Contains(candidates, meld => Consumes(meld, hand[0], hand[1], hand[2]));
        Assert.Contains(candidates, meld => Consumes(meld, hand[0], hand[1], hand[3]));
        Assert.Contains(candidates, meld => Consumes(meld, hand[0], hand[1], hand[2], hand[3]));
    }

    [Fact]
    public void AJokerSubstitutesForASuitTheHandAlreadyHolds()
    {
        // The set counterpart of the run generator's load-bearing case: using the joker for
        // the diamond frees the real 9♦ for another meld (BUILD-PLAN §3.4).
        var hand = Hands.Of("9H", "9S", "9D", "RJ");
        var (heart, spade, diamond, joker) = (hand[0], hand[1], hand[2], hand[3]);

        var candidates = SetGenerator.Candidates(hand);

        Assert.Contains(candidates, meld => Consumes(meld, heart, spade, joker));
        Assert.Contains(candidates, meld => Consumes(meld, heart, spade, diamond, joker));
    }

    [Fact]
    public void AHandOfNothingButJokersStillMakesSets()
    {
        var hand = Hands.Of("RJ", "BJ", "RJ", "BJ");

        var candidates = SetGenerator.Candidates(hand);

        // Four three-card combinations plus the one four-card set — the same shape P3 found
        // for runs. Melds made entirely of jokers are legal only because RULES.md §9 #8
        // recommends *unlimited* jokers per meld; if that settles otherwise, change this test
        // and its RunGenerator twin together.
        Assert.Equal(5, candidates.Count);
        Assert.All(candidates, AssertIsAValidSet);
    }

    [Fact]
    public void TheWorstCaseHandStaysSmall()
    {
        // The largest set-candidate count a 13-card hand can reach: every joker, plus two
        // copies of one rank in every suit but the one with a third copy. Sets are bounded by
        // the four suits, so unlike runs (4,032) they cannot explode.
        var hand = Hands.Of("9H", "9H", "9H", "9S", "9S", "9D", "9D", "9C", "9C",
                            "RJ", "BJ", "RJ", "BJ");

        var candidates = SetGenerator.Candidates(hand);

        // 639 — hundreds, and reached in milliseconds. The arithmetic is closed-form: with
        // suit counts (3,2,2,2) and four jokers, the candidates are every choice of k real
        // cards in distinct suits plus j jokers with 3 ≤ k+j ≤ 4, which is 222 of size three
        // and 417 of size four. No other split of nine real cards over four suits beats it.
        Assert.Equal(639, candidates.Count);
        Assert.All(candidates, AssertIsAValidSet);
    }

    // ---- invariants over every candidate ---------------------------------------------------

    [Fact]
    public void EveryCandidateIsAValidSetWithAUsableInterpretation()
    {
        var hand = Hands.Of("9H", "9S", "9D", "2C", "2D", "2H", "KS", "KC",
                            "AH", "AH", "AS", "RJ", "BJ");

        Assert.All(SetGenerator.Candidates(hand), AssertIsAValidSet);
    }

    [Fact]
    public void NoCandidateIsRepeatedByCardIdSet()
    {
        var hand = Hands.Of("9H", "9H", "9S", "9D", "9C", "RJ", "BJ");

        var candidates = SetGenerator.Candidates(hand);

        var seen = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());
        Assert.All(candidates, meld =>
            Assert.True(seen.Add([.. meld.CardIds]), $"{meld} was generated twice."));
    }

    // ---- cross-check against an independent enumeration ------------------------------------

    [Theory]
    // The four-suit case, a duplicate copy, and a joker.
    [InlineData("9H", "9S", "9D", "9C", "9H", "RJ")]
    // Two ranks that both make sets, two jokers, and cards that make none.
    [InlineData("9H", "9S", "2D", "2C", "2H", "RJ", "BJ", "KS", "10D")]
    // Every joker plus enough of one rank to reach both size limits from either direction.
    [InlineData("AH", "AS", "AD", "AC", "AH", "RJ", "BJ", "RJ", "BJ")]
    public void GeneratorFindsExactlyTheCardSetsThatCanFormASet(params string[] codes)
    {
        // The generator works forwards — rank first, then suit by suit. This works backwards:
        // every subset of the hand, asked whether it could be a set at all. Agreement means
        // the generator neither misses a candidate nor invents one.
        var hand = Hands.Of(codes);

        var generated = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());
        foreach (var meld in SetGenerator.Candidates(hand))
        {
            generated.Add([.. meld.CardIds]);
        }

        var settable = new HashSet<HashSet<CardId>>(HashSet<CardId>.CreateSetComparer());
        foreach (var subset in Subsets(hand))
        {
            if (CouldFormASet(subset))
            {
                settable.Add([.. subset.Select(card => card.Id)]);
            }
        }

        Assert.Equal(settable.Count, generated.Count);
        Assert.True(generated.SetEquals(settable), "The two enumerations disagree.");
    }

    private static IEnumerable<List<Card>> Subsets(IReadOnlyList<Card> hand)
    {
        for (var mask = 0; mask < 1 << hand.Count; mask++)
        {
            var subset = new List<Card>();
            for (var index = 0; index < hand.Count; index++)
            {
                if ((mask & (1 << index)) != 0)
                {
                    subset.Add(hand[index]);
                }
            }

            yield return subset;
        }
    }

    /// <summary>
    /// Whether these cards could be a set: three or four of them, the real ones all of one
    /// rank and all of different suits, jokers covering whatever suits are left. Deliberately
    /// written subset-first, the opposite way round from <see cref="SetGenerator"/>.
    /// </summary>
    private static bool CouldFormASet(List<Card> subset)
    {
        if (subset.Count is < SetGenerator.MinimumSize or > SetGenerator.MaximumSize)
        {
            return false;
        }

        var real = subset.Where(card => !card.IsJoker).ToList();
        if (real.Select(card => card.Rank).Distinct().Count() > 1)
        {
            return false;
        }

        // Distinct suits among the real cards; the jokers take suits none of them occupy,
        // and there is always room because the subset holds at most four cards in total.
        return real.Select(card => card.Suit).Distinct().Count() == real.Count;
    }

    // ---- helpers -----------------------------------------------------------------------------

    private static void AssertIsAValidSet(Meld meld)
    {
        Assert.Equal(MeldKind.Set, meld.Kind);
        Assert.InRange(meld.Count, SetGenerator.MinimumSize, SetGenerator.MaximumSize);
        Assert.Equal(meld.Count, meld.CardIds.Count);

        // One rank throughout, and no suit twice — the whole of RULES.md §6.2, read off the
        // interpretation rather than off the cards, so a joker's stand-in suit counts too.
        Assert.Single(meld.Slots.Select(slot => slot.PlaysAs).Distinct());
        Assert.Equal(meld.Count, meld.Slots.Select(slot => slot.InSuit).Distinct().Count());

        foreach (var slot in meld.Slots.Where(slot => !slot.IsSubstitute))
        {
            // A real card always plays as itself.
            Assert.Equal(slot.PlaysAs, slot.Card.Rank);
            Assert.Equal(slot.InSuit, slot.Card.Suit);
        }
    }

    private static bool Consumes(Meld meld, params Card[] cards) =>
        meld.CardIds.SetEquals(cards.Select(card => card.Id));

    private static void AssertConsumes(Meld meld, params Card[] cards) =>
        Assert.True(Consumes(meld, cards),
            $"Expected [{string.Join(" ", cards.AsEnumerable())}] but the meld was {meld}.");
}
