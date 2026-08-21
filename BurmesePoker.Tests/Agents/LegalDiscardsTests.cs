using BurmesePoker.Domain.Abstractions;
using BurmesePoker.Domain.Agents;
using BurmesePoker.Domain.Cards;
using BurmesePoker.Domain.Play;
using BurmesePoker.Tests.Play;

namespace BurmesePoker.Tests.Agents;

/// <summary>
/// Every seat the computer plays, filtered to the cards the turn actually offers
/// (RULES.md §5.1).
/// </summary>
/// <remarks>
/// <para>
/// 🔥 <b>The whole ladder and the whole dial, not a sample.</b> The ban is enforced by construction
/// rather than by validation, so the claim to be tested is that <em>no way of playing</em> can
/// produce an illegal discard — which is a claim about <c>BotCatalog</c> and
/// <c>DifficultyLadder</c> as a whole, and it stops being true silently the moment somebody adds a
/// rung that ranks the hand instead of the legal cards.
/// </para>
/// <para>
/// ⚠️ <b>The runner-up is the point, not an extra.</b> A difficulty level makes its mistake by
/// throwing the card its rung ranked <em>second</em> (BUILD-PLAN P19), and a mistake still has to
/// be a legal move — so a filter applied to the winner alone would be a dial that quietly cheats
/// at every level but <c>expert</c>.
/// </para>
/// </remarks>
public class LegalDiscardsTests
{
    /// <summary>
    /// Fourteen cards with three Queens in them, so a ban on Queens leaves plenty else to throw.
    /// </summary>
    private static readonly IReadOnlyList<Card> ThreeQueensAndSomeDeadwood =
        Hands.Of("QD", "QC", "QH", "2H", "3H", "4H", "5D", "7C", "9S", "10S", "JS", "AC", "6H", "8D");

    public static TheoryData<string> EveryRung()
    {
        var rungs = new TheoryData<string>();

        foreach (var rung in BotCatalog.All)
        {
            rungs.Add(rung.Name);
        }

        return rungs;
    }

    public static TheoryData<string> EveryLevel()
    {
        var levels = new TheoryData<string>();

        foreach (var level in DifficultyLadder.All)
        {
            levels.Add(level.Name);
        }

        return levels;
    }

    [Theory]
    [MemberData(nameof(EveryRung))]
    public void NoRungThrowsACardTheTurnDidNotOffer(string name)
    {
        var rung = BotCatalog.Resolve(name);

        // Asked many times, because two of the rungs are random and one of them chooses its
        // discard that way.
        for (var seed = 0; seed < 40; seed++)
        {
            var context = TurnContexts.Fed(ThreeQueensAndSomeDeadwood, Hands.Value("QS"));
            var thrown = rung.Create(seed).ChooseDiscard(context);

            Assert.NotEqual(Rank.Queen, thrown.Rank);
            Assert.Contains(context.LegalDiscards, card => card.Id == thrown.Id);
        }
    }

    [Theory]
    [MemberData(nameof(EveryRung))]
    public void NoRungEvenRanksACardTheTurnDidNotOffer(string name)
    {
        if (BotCatalog.Resolve(name).Create(0) is not IRanksDiscards ranker)
        {
            // A rung that does not rank cannot be a difficulty level either, and the test above
            // covers what it does throw.
            return;
        }

        var context = TurnContexts.Fed(ThreeQueensAndSomeDeadwood, Hands.Value("QS"));
        var ranking = ranker.RankDiscards(context);

        Assert.NotEmpty(ranking);
        Assert.All(ranking, card => Assert.NotEqual(Rank.Queen, card.Rank));
        Assert.All(ranking, card => Assert.Contains(context.LegalDiscards, legal => legal.Id == card.Id));
    }

    /// <remarks>
    /// 🔥 The bill §5.1 sends to the difficulty dial. <c>easy</c> slips nine times in ten, so a
    /// runner-up drawn off an unfiltered ranking would feed the seat below on most of its turns.
    /// </remarks>
    [Theory]
    [MemberData(nameof(EveryLevel))]
    public void ADifficultyLevelsMistakeIsAlwaysALegalMove(string name)
    {
        var level = DifficultyLadder.Resolve(name);
        var slipped = 0;

        for (var seed = 0; seed < 60; seed++)
        {
            var context = TurnContexts.Fed(ThreeQueensAndSomeDeadwood, Hands.Value("QS"));
            var player = level.Create(seed);
            var best = ((IRanksDiscards)player).RankDiscards(context)[0];
            var thrown = player.ChooseDiscard(context);

            Assert.NotEqual(Rank.Queen, thrown.Rank);
            Assert.Contains(context.LegalDiscards, card => card.Id == thrown.Id);

            if (thrown.Id != best.Id)
            {
                slipped++;
            }
        }

        // A guard on the guard: at ε = 0 nothing slips, and anywhere else something must, or this
        // would be asserting about a mistake nobody made.
        Assert.Equal(level.MistakeRate > 0, slipped > 0);
    }

    /// <remarks>
    /// The floor, reached through a real player rather than through <c>FeedingBan</c>: a hand of
    /// nothing but banned ranks still completes its turn (RULES.md §5.1, The floor).
    /// </remarks>
    [Theory]
    [MemberData(nameof(EveryRung))]
    public void AHandOfNothingButBannedRanksStillFinishesItsTurn(string name)
    {
        var hand = Hands.Of("QD", "QC", "QH", "QS", "QD", "QC", "QH", "5S", "5H", "5C", "5D", "5S", "5H", "5C");
        var context = TurnContexts.Fed(hand, Hands.Value("QS"), Hands.Value("5D"));

        Assert.Equal(hand.Count, context.LegalDiscards.Count);

        var thrown = BotCatalog.Resolve(name).Create(0).ChooseDiscard(context);

        Assert.Contains(hand, card => card.Id == thrown.Id);
    }

    /// <remarks>
    /// ⚠️ <b>The hint is filtered because it <em>is</em> the rung's answer</b> (P11, P13.1). Nothing
    /// here filters anything: <c>ComputerAdvice</c> asks <c>BotCatalog.Hardest</c> on the very
    /// context the player is looking at, so a hint pointing at an unthrowable card could only come
    /// from a rung that was not filtered — which is what the tests above rule out.
    /// </remarks>
    [Fact]
    public void TheComputersHintIsNeverACardThePlayerCannotThrow()
    {
        var context = TurnContexts.Fed(ThreeQueensAndSomeDeadwood, Hands.Value("QS"));
        var advised = (IPlayerAgent)BotCatalog.Hardest.Create(0);

        Assert.NotEqual(Rank.Queen, advised.ChooseDiscard(context).Rank);
    }
}
